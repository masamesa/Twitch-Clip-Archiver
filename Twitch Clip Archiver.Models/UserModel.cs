namespace Twitch_Clip_Archiver.Models
{
    public class broadcaster
    {
        public string id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string channel_url { get; set; }
        //no idea what type this is, not documented in the API
        public object logo { get; set; }
    }
    public class curator
    {
        public string id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string channel_url { get; set; }
        //no idea what type this is, not documented in the API
        public object logo { get; set; }
    }
    public class vod
    {
        public string id { get; set; }
        public string url { get; set; }
    }
}