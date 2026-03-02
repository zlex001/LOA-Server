using System;
using System.Collections.Generic;
using Data.Config;

namespace Data.Design
{
    // 仅在转换时使用的表达式解析器，运行时不使用
    public class ConditionExpressionParser
    {
        public enum TokenType
        {
            Condition,    // 具体条件如"事件·1章1节0场"
            And,          // &
            Or,           // |
            Not,          // !
            LeftParen,    // (
            RightParen,   // )
            End           // 结束符
        }

        public class Token
        {
            public TokenType Type { get; set; }
            public string Value { get; set; }
            public int Position { get; set; }

            public Token(TokenType type, string value, int position)
            {
                Type = type;
                Value = value;
                Position = position;
            }
        }

        private List<Token> tokens;
        private int currentIndex;
        private Token currentToken;

        public ConditionNode Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return null;

            try
            {
                tokens = Tokenize(expression);
                currentIndex = 0;
                currentToken = tokens[0];
                
                return ParseOrExpression();
            }
            catch (Exception ex)
            {
                throw new Exception($"Condition expression parse error: {ex.Message}");
            }
        }

        private List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            int i = 0;
            
            while (i < expression.Length)
            {
                char c = expression[i];
                
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                switch (c)
                {
                    case '&':
                        tokens.Add(new Token(TokenType.And, "&", i));
                        i++;
                        break;
                    case '|':
                        tokens.Add(new Token(TokenType.Or, "|", i));
                        i++;
                        break;
                    case '!':
                        tokens.Add(new Token(TokenType.Not, "!", i));
                        i++;
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParen, "(", i));
                        i++;
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RightParen, ")", i));
                        i++;
                        break;
                    default:
                        // 读取条件字符串，直到遇到操作符或括号
                        int start = i;
                        while (i < expression.Length && 
                               expression[i] != '&' && 
                               expression[i] != '|' && 
                               expression[i] != '!' && 
                               expression[i] != '(' && 
                               expression[i] != ')')
                        {
                            i++;
                        }
                        
                        string condition = expression.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(condition))
                        {
                            tokens.Add(new Token(TokenType.Condition, condition, start));
                        }
                        break;
                }
            }
            
            tokens.Add(new Token(TokenType.End, "", expression.Length));
            return tokens;
        }

        // 语法分析：递归下降解析
        // 优先级：| 最低，& 较高，! 更高，() 最高
        
        private ConditionNode ParseOrExpression()
        {
            var left = ParseAndExpression();
            
            while (currentToken.Type == TokenType.Or)
            {
                NextToken();
                var right = ParseAndExpression();
                left = new OrCondition(left, right);
            }
            
            return left;
        }

        private ConditionNode ParseAndExpression()
        {
            var left = ParsePrimaryExpression();
            
            while (currentToken.Type == TokenType.And)
            {
                NextToken();
                var right = ParsePrimaryExpression();
                left = new AndCondition(left, right);
            }
            
            return left;
        }

        private ConditionNode ParsePrimaryExpression()
        {
            if (currentToken.Type == TokenType.Not)
            {
                NextToken(); // 跳过 !
                var child = ParsePrimaryExpression();
                return new NotCondition(child);
            }
            else if (currentToken.Type == TokenType.LeftParen)
            {
                NextToken(); // 跳过 (
                var expr = ParseOrExpression();
                
                if (currentToken.Type != TokenType.RightParen)
                {
                    throw new Exception($"Expected ')' at position {currentToken.Position}");
                }
                
                NextToken(); // 跳过 )
                return expr;
            }
            else if (currentToken.Type == TokenType.Condition)
            {
                var condition = currentToken.Value;
                NextToken();
                return new SingleCondition(condition);
            }
            else
            {
                throw new Exception($"Unexpected token '{currentToken.Value}' at position {currentToken.Position}");
            }
        }

        private void NextToken()
        {
            if (currentIndex + 1 < tokens.Count)
            {
                currentIndex++;
                currentToken = tokens[currentIndex];
            }
        }
    }
}
