using HeyRed.Mime;

namespace KmaProxy2
{
    public class StaticDelivery
    {
        public string ContentType { get; set; }
        public Stream ContentStream { get; set; }

        public StaticDelivery(string path) 
        {
            Console.WriteLine("Reading Static Resource: {0}", path);

            ContentStream = File.OpenRead(path);
            ContentType = MimeTypesMap.GetMimeType(path);
        }
    }
}
