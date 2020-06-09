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
        public string path;
        public int totalclips, current;
        public async void Fetch(string ClientID, string TwitchName, string downloadpath, string jsonpath = null)
        {
            string url = $"https://api.twitch.tv/kraken/clips/top?channel={TwitchName}&period=all&trending=false&limit=100"; 
            path = downloadpath;
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
                //garbage check, let this slide, brain no work.
                if (cursor != null)
                    if(cursor != "")
                        url = $"{url + "&cursor=" + cursor}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Accept = "application/vnd.twitchtv.v5+json";
                request.Headers.Add($"Client-ID: {ClientID}");
                var response = await request.GetResponseAsync();
                string resp = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();

                var json = JsonConvert.DeserializeObject<ClipModel>(resp);

                if (json.clips.Length == 0 && clipjson.Count == 0)
                {
                    Console.Write('[');
                    Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
                    Console.ResetColor();
                    Console.Write("] ");
                    Console.WriteLine($"Could not locate any clips for {TwitchName}");
                    return;
                }

                if (json._cursor == startingcursor & cursor != null)
                {
                    int clips = 0;
                    foreach (var clipbundle in clipjson)
                        clips = clips + clipbundle.clips.Count();
                    Console.Clear();
                    Console.WriteLine($"Found {clips} clips!... Starting download!");
                    File.WriteAllText($@".\{json.clips[0].broadcaster.name}.json", JsonConvert.SerializeObject(clipjson));
                    totalclips = clips;
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
                     Console.WriteLine("Invalid client ID or issue with twitch servers!");
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

        public async void Download(string path, int lcount, int currentpos = 0)
        {
            try
            {
                int i = currentpos;
                foreach (ClipModel model in clipjson)
                    
                    foreach (clips clip in model.clips)
                    {
                        i++; current++;
                        string[] url = Regex.Split(clip.thumbnails.medium, "-preview");
                        WebClient wc = new WebClient();
                        string clean = null;
                        //temp fix to remove illegal characters
                        foreach (var c in (new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())))
                            if ($"{clip.broadcaster.name}_{clip.title}_{clip.created_at}".Contains(c.ToString()))
                                clean = $"{clip.broadcaster.name}_{clip.title}_{clip.created_at}".Replace(c, '-').Replace('<', '-').Replace('>', '-').Replace('?', '-').Replace('|', '-').Replace('*', '-');
                        await wc.DownloadFileTaskAsync((new Uri($"{ url[0] + ".mp4" }")), path + '\\' + clean.Trim().Replace(':', '-').Replace('"', '_') + ".mp4");
                        Console.Write('[');
                        Console.Write("✓", Console.ForegroundColor = ConsoleColor.Green);
                        Console.ResetColor();
                        Console.Write("] ");
                        Console.WriteLine($"Downloaded clip {i}/{lcount} - {clip.title}");
                        model.clips = model.clips.Where((source, index) => index != 0).ToArray();
                    }
                clipjson.RemoveAt(0);
            }
            catch(Exception ex)
            {
                Console.Write('[');
                Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
                Console.Write("] ");
                Console.WriteLine("Oh no! An error occured, please screenshot this and show this to masamesa via submitting an issue on github!\r\n" + ex, Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
                
                Console.Write('[');
                Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
                Console.Write("] ");
                Console.WriteLine($"Failed to download clip {current}/{totalclips} - {clipjson.First().clips.First().title}");


                clipjson.First().clips = clipjson.First().clips.Where((source, index) => index != 0).ToArray();

                Console.Write('[');
                Console.Write("✓", Console.ForegroundColor = ConsoleColor.Green);
                Console.ResetColor();
                Console.Write("] ");
                Console.WriteLine($"Recovered! Using backup {Directory.GetCurrentDirectory() + '\\' + clipjson.First().clips.First().broadcaster.name}-backup.json...");


                if (!File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json"))
                    File.Create($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json").Close();

                File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json", JsonConvert.SerializeObject(clipjson));
                Download(path, totalclips, current);

            }
        }
    }
}
