namespace Aizen.Core.Domain.Abstraction.Extention
{
    public class AizenPagination<T> where T : class
    {
        public AizenPagination(int pageIndex, int pageSize, long count, IReadOnlyList<T> data)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            Count = count;
            Data = data;
        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public long Count { get; set; }
        public IReadOnlyList<T> Data { get; set; }
    }
}