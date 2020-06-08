using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Twitch_Clip_Archiver.Extensions
{
    using Twitch_Clip_Archiver.Models;

    public class FetchClips
    {

        public List<ClipModel> clipjson = new List<ClipModel>();
        public async void Fetch(string ClientID, string TwitchName, string downloadpath, string jsonpath = null)
        {
            string url = $"https://api.twitch.tv/kraken/clips/top?channel={TwitchName}&period=all&trending=false&limit=100";
            try
            {
                string cursor = null;
                string startingcursor = null;
                if (jsonpath != null)
                {
                    List<ClipModel> tempj = JsonConvert.DeserializeObject<List<ClipModel>>(File.ReadAllText(jsonpath));
                    if (tempj != null)
                    {
                        clipjson = tempj;
                        startingcursor = clipjson.First()._cursor;
                        cursor = clipjson.Last()._cursor;
                    }
                }


                File.Create($@".\{TwitchName}.json").Close();
            Loop:


                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{url + (cursor != null ? "&cursor=" + cursor : "")}");
                request.Method = "GET";
                request.Accept = "application/vnd.twitchtv.v5+json";
                request.Headers.Add($"Client-ID: {ClientID}");
                var response = await request.GetResponseAsync();
                string resp = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
                var json = JsonConvert.DeserializeObject<ClipModel>(resp);

                if (json._cursor == startingcursor & cursor != null)
                {
                    int clips = 0;
                    foreach (var clipbundle in clipjson)
                        clips = clips + clipbundle.clips.Count();
                    Console.Clear();
                    Console.WriteLine($"Found {clips} clips!... Starting download!");
                    File.WriteAllText($@".\{json.clips[0].broadcaster.name}.json", JsonConvert.SerializeObject(clipjson));
                    Download(downloadpath, clips);
                    return;
                }
                cursor = json._cursor;
                clipjson.Add(json);
                if (startingcursor == null)
                    startingcursor = json._cursor;
                foreach (clips clip in json.clips)
                {
                    Console.Write('[');
                    Console.Write("✓", Console.ForegroundColor = ConsoleColor.Green);
                    Console.ResetColor();
                    Console.Write("] ");
                    Console.WriteLine(clip.url);
                }
                goto Loop;

            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                    Console.WriteLine("Invalid client ID!");
                else
                {
                    Console.Write('[');
                    Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
                    Console.ResetColor();
                    Console.Write("] ");
                    Console.WriteLine("Oh no! An error occured with twitch, please screenshot this and show this to masamesa via submitting an issue on github!\r\n" + ex, Console.ForegroundColor = ConsoleColor.Red);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.Write('[');
                Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
                Console.Write("] ");
                Console.WriteLine("Oh no! An error occured, please screenshot this and show this to masamesa via submitting an issue on github!\r\n" + ex, Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
            }
        }

        public async void Download(string path, int count)
        {
            try
            {
                int i = 0;
                foreach (ClipModel model in clipjson)
                    foreach (clips clip in model.clips)
                    {
                        i++;
                        string[] url = Regex.Split(clip.thumbnails.medium, "-preview");
                        WebClient wc = new WebClient();
                        string clean = null;
                        foreach (var c in Path.GetInvalidFileNameChars())
                            if($"{clip.broadcaster.name}_{clip.title}_{clip.created_at}".Contains(c))
                                clean = $"{clip.broadcaster.name}_{clip.title}_{clip.created_at}".Replace(c, '-');
                        await wc.DownloadFileTaskAsync((new Uri($"{ url[0] + ".mp4" }")), path + '\\' + clean.Replace(':', '-') + ".mp4");

                        Console.Write('[');
                        Console.Write("✓", Console.ForegroundColor = ConsoleColor.Green);
                        Console.ResetColor();
                        Console.Write("] ");
                        Console.WriteLine($"Downloaded clip {i}/{count} - {clip.title}");
                    }
            }
            catch(Exception ex)
            {
                Console.Write('[');
                Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
                Console.Write("] ");
                Console.WriteLine("Oh no! An error occured, please screenshot this and show this to masamesa via submitting an issue on github!\r\n" + ex, Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
            }
        }
    }
}
