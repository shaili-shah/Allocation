using Microsoft.Build.Tasks;
using System.Collections.Generic;

namespace Allocation.ApiModel
{
    public class ResultBase<T>
    {
        public IEnumerable<Error> Errors { get; set; }
        public bool Success { get; set; }
        public string Msg { get; set; }
        public T Data { get; set; }
    }
}