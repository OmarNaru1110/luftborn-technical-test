using System;
using System.Collections.Generic;
using System.Text;

namespace CORE.DTOs
{
    public class ResponseDto<T>
    {
        public T Data { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}