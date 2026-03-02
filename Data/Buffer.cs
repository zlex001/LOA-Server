using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Basic
{
    public class Buffer
    {
        const int DEFAULT_SIZE = 99999;
        public byte[] Content { get; set; }
        public int Read { get; set; }
        public int Write { get; set; }
        private int Capacity { get; set; }
        public int Remain => Capacity - Write;
        public void EnsureCapacity(int required)
        {
            if (required > Capacity)
            {
                int newCapacity = Math.Max(required, Capacity * 2);
                byte[] newContent = new byte[newCapacity];
                Array.Copy(Content, 0, newContent, 0, Write);
                Content = newContent;
                Capacity = newCapacity;
            }
        }
        
        public int ReadableCount => Write - Read;
        public Buffer(int size = DEFAULT_SIZE)
        {
            Content = new byte[size];
            Capacity = size;
            Read = 0;
            Write = 0;
        }
        public void CheckAndMoveBytes()
        {
            if (ReadableCount < 8)
            {
                Compact();
            }
        }
        
        public void Compact()
        {
            if (ReadableCount > 0)
            {
                Array.Copy(Content, Read, Content, 0, ReadableCount);
            }
            Write = ReadableCount;
            Read = 0;
        }
    }
}
