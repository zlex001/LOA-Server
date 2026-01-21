using Newtonsoft.Json;
using System.Net;
using static Domain.Analytics;

namespace Domain.Administrator
{
    public class Analytics
    {
        private static Analytics instance;
        public static Analytics Instance { get { if (instance == null) { instance = new Analytics(); } return instance; } }

        public async void OnAnalytics(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var response = context.Response;
            string startDateParam = null;
            string endDateParam = null;

            try
            {
                var queryString = context.Request.Url.Query;
                var queryParameters = System.Web.HttpUtility.ParseQueryString(queryString);

                startDateParam = queryParameters["startDate"];
                endDateParam = queryParameters["endDate"];

                if (string.IsNullOrEmpty(startDateParam) || string.IsNullOrEmpty(endDateParam))
                {
                    response.StatusCode = 400;
                    response.Close();
                    return;
                }

                DateTime startDate;
                DateTime endDate;

                if (!DateTime.TryParse(startDateParam, out startDate) || !DateTime.TryParse(endDateParam, out endDate))
                {
                    response.StatusCode = 400;
                    response.Close();
                    return;
                }

            if (startDate > endDate)
            {
                response.StatusCode = 400;
                response.Close();
                return;
            }

            var daysDiff = (endDate - startDate).TotalDays;
            if (daysDiff > 365)
            {
                Utils.Debug.Log.Warning("ANALYTICS", $"Query range too large: {daysDiff} days, rejected");
                response.StatusCode = 400;
                await Net.Http.Instance.SendJson(response, new { error = "Query range cannot exceed 365 days", days = daysDiff });
                return;
            }

            List<Daily> dailyDataList;
            
            try
            {
                dailyDataList = Domain.Analytics.Instance.QueryFromDatabase(startDate, endDate);
                
                if (dailyDataList.Count == 0 || dailyDataList.Count < (int)daysDiff + 1)
                {
                    var existingDates = dailyDataList.Select(d => DateTime.Parse(d.Date)).ToHashSet();
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        if (!existingDates.Contains(date))
                        {
                            var computedDaily = new Daily(date);
                            dailyDataList.Add(computedDaily);
                            
                            try
                            {
                                Domain.Analytics.Instance.SaveToDatabase(date);
                            }
                            catch (Exception saveEx)
                            {
                                Utils.Debug.Log.Error("ANALYTICS", $"Failed to save computed data for {date:yyyy-MM-dd}: {saveEx.Message}");
                            }
                        }
                    }
                    
                    dailyDataList = dailyDataList.OrderBy(d => d.Date).ToList();
                }
            }
            catch (Exception)
            {
                dailyDataList = new List<Daily>();
                
                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    Daily daily = new Daily(date);
                    dailyDataList.Add(daily);
                }
            }

            await Net.Http.Instance.SendJson(response, dailyDataList);
        }
        catch (Exception ex)
        {
            Utils.Debug.Log.Error("ANALYTICS", $"[Analytics Query Failed]");
            Utils.Debug.Log.Error("ANALYTICS", $"Date range: {startDateParam} to {endDateParam}");
            Utils.Debug.Log.Error("ANALYTICS", $"Exception: {ex.Message}");
            Utils.Debug.Log.Error("ANALYTICS", $"StackTrace: {ex.StackTrace}");
            
            var empty = new List<Daily>();
            await Net.Http.Instance.SendJson(response, empty);
        }
        }
    }
}

