namespace API.Helpers
{
    public class UserParams
    {
        private const int MaxPageSize = 50; // most amount of things to return from a request
        public int PageNumber { get; set; } = 1; //
        private int _pageSize = 10; // default page size

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value; // limit max page size to 50 if user request max page size is larger
        }

        public string CurrentUsername { get; set; }
        public string Gender { get; set; }
        public int MinAge { get; set; } = 18;
        public int MaxAge { get; set; } = 150;
        public string OrderBy { get; set; } = "lastActive";
    }
}