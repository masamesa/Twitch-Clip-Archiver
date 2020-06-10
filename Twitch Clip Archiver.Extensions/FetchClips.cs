using System;
using System.IO;
using System.Net;
using System.Web;
using System.Linq;
using System.Collections.Generic;
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
            string url = $"https://api.twitch.tv/kraken/clips/top?channel={TwitchName}&period=all&trending=false&limit=100"; //change
            path = downloadpath;
            try
            {
                string cursor = null;
                string startingcursor = null;
                var ps = new ProjectSpecific();
                if (jsonpath != null)
                {
                    List<ClipModel> tempj = JsonConvert.DeserializeObject<List<ClipModel>>(File.ReadAllText(jsonpath));
                    if (tempj != null)
                    {
                        var rcc = await ps.clipCount(tempj);
                        if (tempj.Last()._cursor == "")
                        {
                            clipjson = rcc.Item2;
                            Download(downloadpath, rcc.Item1);
                        }
                        clipjson = rcc.Item2;
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
                    ps.ConsoleRedX($"Could not locate any clips for {TwitchName}", false);
                    return;
                }

                if (json._cursor == startingcursor & cursor != null)
                {
                    var tresp = await ps.clipCount(clipjson);
                    int clips = tresp.Item1;
                    clipjson = tresp.Item2;
                    Console.Clear();
                    Console.WriteLine($"Found {clips} clips!... Starting download!");
                    File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}.json", JsonConvert.SerializeObject(clipjson));
                    totalclips = clips;
                    Download(downloadpath, clips);
                    return;
                }
                //if for some reason the json.clips is empty, don't add it that way there's no chance of empty arrays causing things to break.
                if (json.clips.Length != 0)
                {
                    cursor = json._cursor;
                    clipjson.Add(json);
                    startingcursor = json._cursor;
                    foreach (clips clip in json.clips)
                    {
                        ps.ConsoleGreenCheck($"{clip.url}");
                    }
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
                var ps = new ProjectSpecific();
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
                        ps.ConsoleGreenCheck($"Downloaded clip {i}/{lcount} - {clip.title} - {clip.url.Split('?')[0]}");
                        
                        if (model.clips.Length != 0)
                            model.clips = model.clips.Where((source, index) => index != 0).ToArray();
                        else
                            break;
                    }


                 if (clipjson.Count != 0)
                    clipjson.RemoveAt(0);
                 else
                 {
                    ps.ConsoleGreenCheck("Finished downloading!");
                    return;
                 }

            }
            catch (Exception ex)
            {
                try
                {
                    var ps = new ProjectSpecific();

                    ps.ConsoleRedX("Oh no! An error occured, please screenshot this and show this to masamesa via submitting an issue on github!\r\n" + ex, true);

                    //error handling if for whatever reason the clipjson.clips were to be empty.
                    if (clipjson.First().clips.Length != 0)
                    {
                        ps.ConsoleRedX($"Failed to download clip {current}/{totalclips} - {clipjson.First().clips.First().title}", false);

                        clipjson.First().clips = clipjson.First().clips.Where((source, index) => index != 0).ToArray();
                    }
                    else
                    {
                        clipjson.RemoveAt(0);
                    }

                    ps.ConsoleGreenCheck($"Recovered! Using backup {Directory.GetCurrentDirectory() + '\\' + clipjson.First().clips.First().broadcaster.name}-backup.json...");


                    if (!File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json"))
                        File.Create($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json").Close();

                    File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json", JsonConvert.SerializeObject(clipjson));
                    Download(path, totalclips, current);
                }
                catch(Exception err)
                {
                    var ps = new ProjectSpecific();

                    ps.ConsoleRedX($"EMERGENCY BACKUP MADE.\r\nPLEASE TRY AGAIN AND SUBMIT AN ISSUE ON GITHUB, THIS IS VERY ABNORMAL.\r\n{err}", true);
                    
                    ps.ConsoleGreenCheck($"Recovered! Using backup {Directory.GetCurrentDirectory() + '\\' + clipjson.First().clips.First().broadcaster.name}-backup.json...");

                    if (!File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json"))
                        File.Create($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json").Close();

                    File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json", JsonConvert.SerializeObject(clipjson));
                }
            }
        }
    }
}
