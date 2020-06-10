using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Twitch_Clip_Archiver.Extensions
{
    using System.Runtime.CompilerServices;
    using Twitch_Clip_Archiver.Models;
    class ProjectSpecific
    {
        public Task<Tuple<int, List<ClipModel>>> clipCount(List<ClipModel> clipcomp)
        {
            int clips = 0;
            int i = 0;
            foreach (var cliparray in clipcomp)
            {
                if (cliparray.clips.Length != 0) 
                    clips = clips + cliparray.clips.Count();
                else
                {
                    clipcomp.RemoveAt(i);
                    break;
                }
                i++;
            }
            return Task.FromResult(new Tuple<int, List<ClipModel>>(clips, clipcomp));
        }

        public void ConsoleRedX (string output, bool redoutput)
        {
            Console.Write('[');
            Console.Write("X", Console.ForegroundColor = ConsoleColor.Red);
            Console.ResetColor();
            Console.Write("] ");
            if (redoutput)
            {
                Console.WriteLine(output, Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
            }
            else
                Console.WriteLine(output);
        }
        public void ConsoleGreenCheck(string output)
        {
            Console.Write('[');
            Console.Write("✓", Console.ForegroundColor = ConsoleColor.Green);
            Console.ResetColor();
            Console.Write("] ");
            Console.WriteLine(output);
        }
    }
}
