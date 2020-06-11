using System;
using System.IO;
using System.Net;
using System.Web;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using HtmlAgilityPack;
using OpenQA.Selenium;

namespace Twitch_Clip_Archiver.Extensions
{
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.IE;
    using OpenQA.Selenium.Support.UI;
    using System.Text;
    using Twitch_Clip_Archiver.Models;

    public class FetchClips
    {

        public List<ClipModel> clipjson = new List<ClipModel>();
        public List<ClipModel> Failedclipsjson = new List<ClipModel>();

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
                            return;
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
                var ps = new ProjectSpecific();
                var dt = DateTime.Now;
                File.WriteAllText($@".\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt", ex.ToString());
                if (ex.Status == WebExceptionStatus.ProtocolError)
                    ps.ConsoleRedX("Invalid client ID or issue with twitch servers!", true);
                else
                {
                    ps.ConsoleRedX($"Oh no! An error occured with twitch! Please screenshot this and provide the crash log located at ({Directory.GetCurrentDirectory() })\\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt)" +
                                    $"to masamesa via submitting an issue on github!\r\n" + ex, true);
                }
            }
            catch (Exception ex)
            {
                var dt = DateTime.Now;
                File.WriteAllText($@".\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt", ex.ToString());
                new ProjectSpecific().ConsoleRedX($"Oh no! An error occured! Please screenshot this and provide the crash log located at ({Directory.GetCurrentDirectory() })\\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt)" +
                                    $"to masamesa via submitting an issue on github!\r\n" + ex, true);
            }
        }

        public void Download(string path, int lcount, int currentpos = 0)
        {
            try
            {
                var ps = new ProjectSpecific();
                int i = currentpos; 
                foreach (ClipModel model in clipjson)
                    
                    foreach (clips clip in model.clips)
                    {
                        int retry = 0;
                        i++; current++;
                        string[] url = Regex.Split(clip.thumbnails.medium, "-preview");
                        url[0] = url[0] + ".mp4";
                        if (!Regex.IsMatch(url[0], "offset"))
                        {
                            Retry:
                            var htmldoc = new HtmlDocument();
                            FirefoxOptions op = new FirefoxOptions();
                            op.AddArgument("--headless");
                            op.SetLoggingPreference("driver", LogLevel.Off);
                            
                            IWebDriver driver = new FirefoxDriver(op);

                            driver.Navigate().GoToUrl("https://clips.twitch.tv/embed?clip=" + clip.slug);
                            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                            htmldoc.LoadHtml(driver.PageSource);
                            driver.Quit();
                            retry++;
                            var node = htmldoc.DocumentNode.SelectSingleNode("//div[@class='clips-embed-page tw-c-background-base tw-full-height']/div[@class='video-player']/div[@class='tw-absolute tw-bottom-0 tw-left-0 tw-overflow-hidden tw-right-0 tw-top-0 video-player__container']/video/@src");
                            if (retry == 3)
                            {
                                ps.ConsoleRedX($"Failed to download clip {current}/{totalclips} - {clipjson.First().clips.First().title}", false);
                                clips[] carray = { clipjson.First().clips.First() };
                                if (File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-faileddump.json"))
                                    Failedclipsjson = JsonConvert.DeserializeObject<List<ClipModel>>(File.ReadAllText($@".\{clipjson.First().clips.First().broadcaster.name}-faileddump.json"));
                                Failedclipsjson.Add(new ClipModel { clips = carray, _cursor = "" });
                                File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-faileddump.json", JsonConvert.SerializeObject(Failedclipsjson));

                                clipjson.First().clips = clipjson.First().clips.Where((source, index) => index != 0).ToArray();
                                goto End;
                            }
                            if (node == null)
                                goto Retry;
                            url[0] = node.Attributes[2].Value;
                        }
                        WebClient wc = new WebClient();
                        string clean = null;
                        //temp fix to remove illegal characters
                        foreach (var c in (new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())))
                            if ($"{clip.broadcaster.name}_{clip.title}_{clip.created_at}".Contains(c.ToString()))
                                clean = $"{clip.broadcaster.name}_{clip.title}_{clip.created_at}".Replace(c, '-');
                        clean = clean.Replace('<', '-').Replace('>', '-').Replace('?', '-').Replace('|', '-').Replace('*', '-').Replace(':', '-').Replace('"', '_').Trim();

                        wc.DownloadFile(new Uri(url[0]), path + '\\' + clean + ".mp4");

                        ps.ConsoleGreenCheck($"Downloaded clip {i}/{lcount} - {clip.title} - {clip.url.Split('?')[0]}");
                        End:
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
                    var dt = DateTime.Now;

                    ps.ConsoleRedX($"Oh no! An error occured! Please screenshot this and provide the crash log located at ({Directory.GetCurrentDirectory() })\\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt)" +
                                    $"to masamesa via submitting an issue on github!\r\n" + ex, true);
                    File.WriteAllText($@".\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt", ex.ToString());
                    //error handling if for whatever reason the clipjson.clips were to be empty.
                    if (clipjson.First().clips.Length != 0)
                    {
                        ps.ConsoleRedX($"Failed to download clip {current}/{totalclips} - {clipjson.First().clips.First().title}", false);
                        clips[] carray = { clipjson.First().clips.First() };
                        if (File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-faileddump.json"))
                            Failedclipsjson = JsonConvert.DeserializeObject<List<ClipModel>>(File.ReadAllText($@".\{clipjson.First().clips.First().broadcaster.name}-faileddump.json"));
                        Failedclipsjson.Add(new ClipModel { clips = carray, _cursor = "" });
                        File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-faileddump.json", JsonConvert.SerializeObject(Failedclipsjson));

                        clipjson.First().clips = clipjson.First().clips.Where((source, index) => index != 0).ToArray();
                    }
                    else
                        clipjson.RemoveAt(0);

                    ps.ConsoleGreenCheck($"Recovered! Using backup {Directory.GetCurrentDirectory() + '\\' + clipjson.First().clips.First().broadcaster.name}-backup.json...");


                    if (!File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json"))
                        File.Create($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json").Close();

                    File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json", JsonConvert.SerializeObject(clipjson));
                    Download(path, totalclips, current);
                }
                catch(Exception err)
                {
                    var ps = new ProjectSpecific();
                    var dt = DateTime.Now;

                    ps.ConsoleRedX($"EMERGENCY BACKUP MADE.\r\nPLEASE TRY AGAIN AND SUBMIT AN ISSUE ON GITHUB WITH A SCREENSHOT AND THE CRASH REPORT LOCATED AT ({Directory.GetCurrentDirectory() })\\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt), THIS IS VERY ABNORMAL.\r\n{err}", true);
                    File.WriteAllText($@".\CrashReport_{dt.ToString().Replace(':', '-').Replace('/', '-')}.txt", err.ToString());

                    ps.ConsoleGreenCheck($"Recovered! Using backup {Directory.GetCurrentDirectory() + '\\' + clipjson.First().clips.First().broadcaster.name}-backup.json...");

                    if (!File.Exists($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json"))
                        File.Create($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json").Close();

                    File.WriteAllText($@".\{clipjson.First().clips.First().broadcaster.name}-backup.json", JsonConvert.SerializeObject(clipjson));
                }
            }
        }
    }
}
