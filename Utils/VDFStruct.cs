using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TEKLauncher.Utils
{
    internal class VDFStruct
    {
        internal VDFStruct() { }
        internal VDFStruct(StreamReader Reader)
        {
            string Line = Reader.ReadLine();
            Key = Line.Substring(1, Line.Length - 2);
            Parse(Reader);
        }
        internal string Key, Value;
        internal List<VDFStruct> Children = new List<VDFStruct>();
        internal VDFStruct this[string Key] => Children.Find(VDFStruct => VDFStruct.Key == Key);
        internal void Parse(StreamReader Reader)
        {
            string Line;
            while (!(Reader.EndOfStream || (Line = Reader.ReadLine()).Contains("}")))
            {
                int QMC = Line.Count(Symbol => Symbol == '"');
                if (QMC == 0)
                    continue;
                string[] Fields = Line.Split('"');
                if (QMC == 2)
                {
                    VDFStruct Child = new VDFStruct { Key = Fields[1] };
                    Child.Parse(Reader);
                    Children.Add(Child);
                }
                else if (QMC == 4)
                    Children.Add(new VDFStruct { Key = Fields[1], Value = Fields[3] });
            }
        }
    }
}