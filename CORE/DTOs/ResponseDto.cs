using System;
using System.Collections.Generic;
using System.Text;
using CORE.Enums;

namespace CORE.DTOs
{
    public class ResponseDto<T>
    {
        public T Data { get; set; }
        public ResultStatus Status { get; set; }
        public string Message { get; set; }
    }
}