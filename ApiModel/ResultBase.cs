namespace Allocation.ApiModel
{
    public class ResultBase<T>
    {
        public bool Success { get; set; }
        public string Msg { get; set; }
        public T Data { get; set; }
    }
}