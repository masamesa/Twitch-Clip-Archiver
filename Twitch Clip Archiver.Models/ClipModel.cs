namespace Twitch_Clip_Archiver.Models
{
    public class ClipModel
    {
        public clips[] clips { get; set; }
        public string _cursor { get; set; }
    }
    public class clips
    {
        public string slug { get; set; }
        public string tracking_id { get; set; }
        public string url { get; set; }
        public string embed_url { get; set; }
        public string embed_html { get; set; }
        public broadcaster broadcaster { get; set; }
        public curator curator { get; set; }
        public vod vod { get; set; }
        public string game { get; set; }
        public string language { get; set; }
        public string title { get; set; }
        public long views { get; set; }
        public float duration { get; set; }
        public string created_at { get; set; }
        public string thumbnail_url { get; set; }
        public thumbnails thumbnails { get; set; }
    }

    public class thumbnails
    {
        public string medium { get; set; }
        public string small { get; set; }
        public string tiny { get; set; }

    }

    public class tc
    {
        public string quality { get; set; }
        public string framerate { get; set; }
        public string url { get; set; }

        public tc(string Quality, string Framerate, string Url)
        {
            quality = Quality;
            framerate = Framerate;
            url = Url;
        }
    }
}