using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PickTimeRetard
{
    static class Program
    {

        private static PerformanceCounter perfc;
        
        static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            perfc = new PerformanceCounter("Процессор", "% загруженности процессора", "_Total", true);
            Point prevpos = new Point(null), curpos;
            IPoint raw_amplie_loc = new IPoint();
            SPoint raw_min = new SPoint(1), raw_max = new SPoint(0),
                amplie = new SPoint(2), amplie_loc = new SPoint(2);
            short tint;
            byte cp = 0, raw_counter = 0, 
                counter_freq = 0;
            bool ffrc = false;
            //Initialise
            LoadSettings();
            for (byte i = 0; i != nthc; i++)
            {
                rth.Add(new Thread(RetardTh));
                rth.Last().Priority = ThreadPriority.Lowest;
            }
            //End Initialise
            while (true)
            {
                GetCursorPos(out curpos);
                if (curpos.x != prevpos.x || curpos.y != prevpos.y)
                {
                    raw_counter++;
                    raw_amplie_loc.x += (short)curpos.x;
                    raw_amplie_loc.y += (short)curpos.y;
                    if (raw_min.x > curpos.x) raw_min.x = (short)curpos.x;
                    if (raw_min.y > curpos.y) raw_min.y = (short)curpos.y;
                    if (raw_max.x < curpos.x) raw_max.x = (short)curpos.x;
                    if (raw_max.y < curpos.y) raw_max.y = (short)curpos.y;
                }
                if (cp == 50)
                {
                    if (raw_counter > 1)
                    {
                        amplie.x = (short)Math.Abs(raw_max.x - raw_min.x);
                        amplie.y = (short)Math.Abs(raw_max.y - raw_min.y);
                        tint = (short)(raw_amplie_loc.x / raw_counter - raw_min.x);
                        amplie_loc.x = Math.Abs(tint);
                        tint = (short)(raw_amplie_loc.y / raw_counter - raw_min.y);
                        amplie_loc.y = Math.Abs(tint);
                        if (ffrc) counter_freq = (byte)(raw_counter - 1);
                        else { counter_freq = raw_counter; ffrc = true; }
                    }
                    else counter_freq = 0;
                    raw_amplie_loc.x = (short)curpos.x; raw_amplie_loc.y = (short)curpos.y;
                    raw_min.x = short.MaxValue; raw_min.y = short.MaxValue; raw_max.x = short.MinValue; raw_max.y = short.MinValue;
                    raw_counter = 1;
                    cp = 0;
                    if (noRetardmode) { ncurrpos.Add(new Nscstep(counter_freq, amplie, amplie_loc)); PrepareActivity(); }
                    else { if (nrpos) { nretardpos = new Nscstep(counter_freq, amplie, amplie_loc); nrpos = false; } RetardActivity(); }
                }
                else cp++;
                prevpos = curpos;
                Thread.Sleep(250);
            }
        }
        static List<Thread> rth = new List<Thread>();
        static byte nthc = 1; //to write
        static ushort retardtimer = 150;
        static bool noRetardmode = true;

        static byte acc_range_freq = 10, acc_range_size = 15, acc_range_pivot = 8;
        static List<Nscstep> ncurrpos = new List<Nscstep>();
        static Nscstep nretardpos; static bool nrpos = true;
        static List<NSic> ndatalowork = new List<NSic>(); //to write
        static byte rewaittime = 71;
        static ushort maxseqlen = 10;
        static int ndta_index;
        static void PrepareActivity()
        {
            if (nrpos)
            {
                if (rewaittime == 80)
                {
                    ndta_index = -1;
                    //Compare siquense
                    for (ushort i = 0; i != ndatalowork.Count; i++)
                        if (ncurrpos.Last().Equals(ndatalowork[i].sequence[ndatalowork[i].eqlevpos])) {
                            ndatalowork[i].eqlevpos++;
                            if (ndatalowork[i].eqlevpos == ndatalowork[i].sequence.Count) { ndta_index = i; break; }
                        } else ndatalowork[i].eqlevpos = 0;
                    for (ushort i = 0; i != ndatalowork.Count; i++) ndatalowork[i].eqlevpos = 0;
                    //End Compare siquense
                    if (ndta_index == -1)
                    {
                        if (ncurrpos.Last().freq > 0)
                        {
                            if (GrandomU(0, retardtimer) == 0)
                            {
                                rewaittime = 0;
                                byte fq = Calc_freqcoefcurrpos();
                                if (fq > 95)
                                    retardtimer = 13;
                                else if (fq > 83)
                                    retardtimer = 5;
                                else if (fq > 55)
                                    retardtimer = 2;
                                else if (fq > 25)
                                    retardtimer = 1;
                                else
                                    retardtimer = 0;
                                //Call Retard
                                noRetardmode = false;
                                foreach (Thread i in rth)
                                    i.Start();
                                firstra = true;
                                //End Call Retard
                                //Age inspector
                                for (ushort i = 0; i != ndatalowork.Count; i++)
                                    if (ndatalowork[i].sequence.Count > 1) {
                                        if (++ndatalowork[i].age > 50) { ndatalowork[i].sequence.RemoveAt(0); ndatalowork[i].age = 25; if (ndatalowork[ndta_index].sequence.Count < 7) ndatalowork[i].Calc_freqcoef(); i -= Optimise(i); }
                                    } else if (++ndatalowork[i].age > 100) { ndatalowork.RemoveAt(i); i--; }
                                 FindMaxsqlen();
                                //End Age inspector
                            }
                            retardtimer--;
                        } else retardtimer = 150;
                    }
                    else
                    {
                        rewaittime = 0;
                        byte fq = Calc_freqcoefcurrpos();
                        if (fq > 95)
                            retardtimer = 13;
                        else if (fq > 83)
                            retardtimer = 5;
                        else if (fq > 55)
                            retardtimer = 2;
                        else if (fq > 25)
                            retardtimer = 1;
                        else
                            retardtimer = 0;
                        //Call Retard
                        noRetardmode = false;
                        foreach (Thread i in rth)
                            i.Start();
                        firstra = true;
                        //End Call Retard
                        //Age inspector
                        for (ushort i = 0; i != ndatalowork.Count; i++)
                            if (i == ndta_index)
                                ndatalowork[i].age = 0;
                            else {
                                if (ndatalowork[i].sequence.Count > 1) {
                                    if (++ndatalowork[i].age > 50) { ndatalowork[i].sequence.RemoveAt(0); ndatalowork[i].age = 25; if (ndatalowork[ndta_index].sequence.Count < 7) ndatalowork[i].Calc_freqcoef(); i -= Optimise(i); } 
                                } else if (++ndatalowork[i].age > 100) { ndatalowork.RemoveAt(i); i--; }
                            }
                        FindMaxsqlen();
                        //End Age inspector
                    }
                    if (ncurrpos.Count > maxseqlen) do { ncurrpos.RemoveAt(0); } while (ncurrpos.Count > maxseqlen); 
                } else rewaittime++;
            }
            else
            {
                nrpos = true;
                byte fq = Calc_freqcoefcurrpos(), rfq = Calc_freqcoefone(nretardpos.freq);
                if (ndta_index == -1) {
                    if (rfq > (fq == 102 ? 101 : fq) && ndatalowork.Count < 4096)
                    {
                        ndatalowork.Add(new NSic(ncurrpos.GetRange(ncurrpos.Count - 10, 10).ToList()));
                        ndatalowork.Last().freqcoef = fq;
                        Optimise((ushort)(ndatalowork.Count - 1));
                        FindMaxsqlen();
                    }
                } else {
                    if (fq > ndatalowork[ndta_index].freqcoef) {
                        if (rfq > (fq == 102 ? 101 : fq))
                        {
                            if (ndatalowork[ndta_index].trlevel < 20)
                            {
                                ndatalowork[ndta_index].trlevel++;
                            }
                            if (ndatalowork[ndta_index].sequence.Count < 1024)
                                if (ndatalowork[ndta_index].need_segment)
                                {
                                    ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                    ndatalowork[ndta_index].need_segment = false;
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    Optimise((ushort)ndta_index);
                                    FindMaxsqlen();
                                }
                                else
                                {
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    if (ndatalowork[ndta_index].stablegraph == 10 || ndatalowork[ndta_index].stablegraph == 5)
                                    {
                                        ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                        Optimise((ushort)ndta_index);
                                        FindMaxsqlen();
                                    }
                                }
                        } else if (rfq > (ndatalowork[ndta_index].freqcoef == 102 ? 101 : ndatalowork[ndta_index].freqcoef)) {
                            if (ndatalowork[ndta_index].sequence.Count < 1024)
                                if (ndatalowork[ndta_index].need_segment)
                                {
                                    ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                    ndatalowork[ndta_index].need_segment = false;
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    Optimise((ushort)ndta_index);
                                    FindMaxsqlen();
                                }
                                else
                                {
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    if (ndatalowork[ndta_index].stablegraph == 10 || ndatalowork[ndta_index].stablegraph == 5)
                                    {
                                        ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                        Optimise((ushort)ndta_index);
                                        FindMaxsqlen();
                                    }
                                }
                        } else {
                            if (ndatalowork[ndta_index].trlevel == 0) {
                                ndatalowork.RemoveAt(ndta_index);
                                FindMaxsqlen();
                            } else {
                                ndatalowork[ndta_index].trlevel--;
                                ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                ndatalowork[ndta_index].stablegraph &= 15;
                                if (ndatalowork[ndta_index].stablegraph == 10 || ndatalowork[ndta_index].stablegraph == 5)
                                    ndatalowork[ndta_index].need_segment = true;
                            }
                        }
                    } else {
                        if (rfq > (ndatalowork[ndta_index].freqcoef == 102 ? 101 : ndatalowork[ndta_index].freqcoef)) {
                            if (ndatalowork[ndta_index].trlevel < 20)
                            {
                                ndatalowork[ndta_index].trlevel++;
                            }
                            if (ndatalowork[ndta_index].sequence.Count < 1024)
                                if (ndatalowork[ndta_index].need_segment)
                                {
                                    ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                    ndatalowork[ndta_index].need_segment = false;
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    Optimise((ushort)ndta_index);
                                    FindMaxsqlen();
                                }
                                else
                                {
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    if (ndatalowork[ndta_index].stablegraph == 10 || ndatalowork[ndta_index].stablegraph == 5)
                                    {
                                        ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                        Optimise((ushort)ndta_index);
                                        FindMaxsqlen();
                                    }
                                }
                        } else if (rfq > (fq == 102 ? 101 : fq)) {
                            if (ndatalowork[ndta_index].sequence.Count < 1024)
                                if (ndatalowork[ndta_index].need_segment)
                                {
                                    ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                    ndatalowork[ndta_index].need_segment = false;
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    Optimise((ushort)ndta_index);
                                    FindMaxsqlen();
                                }
                                else
                                {
                                    ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                    ndatalowork[ndta_index].stablegraph &= 15;
                                    ndatalowork[ndta_index].stablegraph |= 1;
                                    if (ndatalowork[ndta_index].stablegraph == 10 || ndatalowork[ndta_index].stablegraph == 5)
                                    {
                                        ndatalowork[ndta_index].sequence.Insert(0, ncurrpos[ncurrpos.Count - ndatalowork[ndta_index].sequence.Count - 1]); if (ndatalowork[ndta_index].sequence.Count < 8) ndatalowork[ndta_index].Calc_freqcoef();
                                        Optimise((ushort)ndta_index);
                                        FindMaxsqlen();
                                    }
                                }
                        } else {
                            if (ndatalowork[ndta_index].trlevel == 0)
                            {
                                ndatalowork.RemoveAt(ndta_index);
                                FindMaxsqlen();
                            }
                            else
                            {
                                ndatalowork[ndta_index].trlevel--;
                                ndatalowork[ndta_index].stablegraph = (byte)(ndatalowork[ndta_index].stablegraph << 1);
                                ndatalowork[ndta_index].stablegraph &= 15;
                                if (ndatalowork[ndta_index].stablegraph == 10 || ndatalowork[ndta_index].stablegraph == 5)
                                    ndatalowork[ndta_index].need_segment = true;
                            }
                        }
                    }
                }
                SaveSettings();
            }
        }
        
        static ushort Optimise(ushort rel_elem)
        {
            ushort stelem = rel_elem, npos = 0;
            optistart:;
            NSic elem = ndatalowork[rel_elem];
            for (ushort i = 0; i != ndatalowork.Count; i++)
                if (i != rel_elem) {
                    for (int i1 = 0; i1 != elem.sequence.Count; i1++)
                        if (i1 == ndatalowork[i].sequence.Count) { rel_elem = i; goto optistart; }
                        else if (!elem.sequence[i1].Equals(ndatalowork[i].sequence[i1])) goto nextstep;
                    if (elem.sequence.Count == ndatalowork[i].sequence.Count)
                    {
                        if (elem.trlevel >= ndatalowork[i].trlevel) {
                            if (elem.age > ndatalowork[i].age) { ndatalowork[rel_elem].age = elem.age = ndatalowork[i].age; }
                            ndatalowork.RemoveAt(i); i--;
                            if (i < rel_elem) rel_elem--;
                            if (stelem <= i) npos++;
                        } else { rel_elem = i; goto optistart; }
                    }
                    else
                    {
                        if (elem.trlevel >= ndatalowork[i].trlevel)
                        {
                            if (elem.age > ndatalowork[i].age) { ndatalowork[rel_elem].age = elem.age = ndatalowork[i].age; }
                            ndatalowork.RemoveAt(i); i--;
                            if (i < rel_elem) rel_elem--;
                            if (stelem <= i) npos++;
                        }
                    }
                    nextstep:;
                }
            return npos;
        }
        static bool firstra = true;
        static void RetardActivity()
        {
            if (retardtimer == 0)
            {
                if (firstra) { perfc.NextValue(); firstra = false; }
                else if (perfc.NextValue() < 100 && nthc < 255) nthc++;
                foreach (Thread i in rth)
                    i.Abort();
                retardtimer = 150;
                noRetardmode = true;
                SaveSettings();
            }
            else
            {
                if (firstra) { perfc.NextValue(); firstra = false; }
                else if (perfc.NextValue() < 100 && nthc < 255)
                {
                    rth.Add(new Thread(RetardTh));
                    rth.Last().Priority = ThreadPriority.Lowest;
                    rth.Last().Start();
                    nthc++;
                }
                retardtimer--;
            }
        }

        static void RetardTh()
        {
#pragma warning disable CS0219
            long num;
            while (true)
            {
                num = long.MaxValue / 3;
                num = long.MinValue / 7;
                num = (long.MaxValue - 14) / 13;
                num = (long.MinValue + 24) / 17;
            }
#pragma warning restore CS0219
        }
        static void LoadSettings()
        {
            if (!System.IO.File.Exists(Environment.CurrentDirectory + @"\ndata.dat")) return;
            System.IO.StreamReader sr = new System.IO.StreamReader(Environment.CurrentDirectory + @"\ndata.dat", System.Text.Encoding.ASCII);
            if (sr.EndOfStream) { sr.Close(); return; }
            nthc = byte.Parse(sr.ReadLine());
            if (sr.EndOfStream) { sr.Close(); return; }
            string str = sr.ReadLine();
            if (str[0] == '>')
            {
                NSic dt; int[] arr;
                dt = new NSic(new List<Nscstep>());
                arr = str.Substring(1).Split(',').Select(x => int.Parse(x)).ToArray();
                dt.trlevel = (byte)arr[0];
                dt.age = (byte)arr[1];
                dt.stablegraph = (byte)arr[2];
                dt.freqcoef = (byte)arr[3];
                dt.need_segment = arr[4] != 0;
                while (!sr.EndOfStream)
                {
                    str = sr.ReadLine();
                    if (str[0] == '>')
                    {
                        ndatalowork.Add(dt);
                        dt = new NSic(new List<Nscstep>());
                        arr = str.Substring(1).Split(',').Select(x => int.Parse(x)).ToArray();
                        dt.trlevel = (byte)arr[0];
                        dt.age = (byte)arr[1];
                        dt.stablegraph = (byte)arr[2];
                        dt.freqcoef = (byte)arr[3];
                        dt.need_segment = arr[4] != 0;
                    }
                    else
                    {
                        arr = str.Split(',').Select(x => int.Parse(x)).ToArray();
                        dt.sequence.Add(new Nscstep((byte)arr[0], new SPoint((short)arr[1], (short)arr[2]), new SPoint((short)arr[3], (short)arr[4])));
                    }
                }
                ndatalowork.Add(dt);
            }
            sr.Close();
        }
        static void SaveSettings()
        {
            System.IO.StreamWriter sr = new System.IO.StreamWriter(Environment.CurrentDirectory + @"\ndata.dat", false, System.Text.Encoding.ASCII);
            sr.WriteLine(nthc);
            foreach (NSic dt in ndatalowork)
            {
                sr.WriteLine(">" + dt.trlevel + "," + dt.age + "," + dt.stablegraph + "," + dt.freqcoef + "," + (dt.need_segment ? "1" : "0"));
                foreach (Nscstep ns in dt.sequence)
                    sr.WriteLine(ns.freq + "," + ns.amply.x + "," + ns.amply.y + "," + ns.pivot.x + "," + ns.pivot.y);
            }
            sr.Close();
        }
        static void FindMaxsqlen()
        {
            maxseqlen = (ushort)(ndatalowork.Count == 0 ? 10 : ndatalowork.Max(x => x.sequence.Count) + 1); if (maxseqlen < 10) maxseqlen = 10;
        }
        static byte Calc_freqcoefone(byte num)
        {
            byte summ = num, dkoef = 1;
            for (; dkoef != 7; dkoef++)
                summ += (byte)Math.Round(num / (Math.Pow(2, dkoef)));
            return summ;
        }
        static byte Calc_freqcoefcurrpos()
        {
            byte summ = 0, dkoef = 0;
            if (ncurrpos.Count < 7)
            {
                ushort midk = 0;
                for (byte i = 0; i != ncurrpos.Count; i++)
                    midk += ncurrpos[i].freq;
                midk = (ushort)Math.Round((double)midk / ncurrpos.Count);
                for (SByte i = (SByte)(ncurrpos.Count - 1); i != -1; i--, dkoef++)
                    summ += (byte)Math.Round(ncurrpos[i].freq / (Math.Pow(2, dkoef)));
                for (; dkoef != 7; dkoef++)
                    summ += (byte)Math.Round(midk / (Math.Pow(2, dkoef)));
            }
            else
                for (short i = (short)(ncurrpos.Count - 1); i != ncurrpos.Count - 8; i--, dkoef++)
                    summ += (byte)Math.Round(ncurrpos[i].freq / (Math.Pow(2, dkoef)));
            return summ;
        }
        static System.Security.Cryptography.RNGCryptoServiceProvider rand = new System.Security.Cryptography.RNGCryptoServiceProvider();
        public static ushort GrandomU(ushort min_val, ushort max_val)
        {
            byte[] b1 = new byte[2];
            rand.GetBytes(b1);
            ushort result = (ushort)Math.Round((double)(BitConverter.ToUInt16(b1, 0) * (max_val - min_val + 1) / 65535 + min_val));
            return (result > max_val ? (ushort)(result - 1) : result);
        }
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point lpPoint);
        [StructLayout(LayoutKind.Sequential)]
        struct Point { public int x, y;  public Point(object o) { x = 0; y = 0; } }
        class SPoint
        {
            public short x, y;
            public SPoint(ushort m)
            {
                if (m == 0) { x = short.MinValue; y = short.MinValue; }
                else if (m == 1) { x = short.MaxValue; y = short.MaxValue; }
                else { x = 0; y = 0; }
            }
            public SPoint(short x, short y) { this.x = x; this.y = y; }
            public SPoint Clone() { return new SPoint(x, y); }
        }
        class IPoint { public int x = 0, y = 0; }
        class NSic
        {
            public List<Nscstep> sequence;
            public byte trlevel = 2; //create with 2, 0 to del, 20 to save
            public byte freqcoef = 0;
            public byte age = 0, stablegraph = 1;
            public ushort eqlevpos = 0;
            public bool need_segment = false;
            public NSic(List<Nscstep> sequence)
            {
                this.sequence = sequence;
            }
            public void Calc_freqcoef()
            {
                byte summ = 0, dkoef = 0;
                if (sequence.Count < 7)
                {
                    ushort midk = 0;
                    for (byte i = 0; i != sequence.Count; i++)
                        midk += sequence[i].freq;
                    midk = (ushort)Math.Round((double)midk / sequence.Count);
                    for (SByte i = (SByte)(sequence.Count - 1); i != -1; i--, dkoef++)
                        summ += (byte)Math.Round(sequence[i].freq / (Math.Pow(2, dkoef)));
                    for (; dkoef != 7; dkoef++)
                        summ += (byte)Math.Round(midk / (Math.Pow(2, dkoef)));
                }
                else
                    for (short i = (short)(sequence.Count - 1); i != sequence.Count - 8; i--, dkoef++)
                        summ += (byte)Math.Round(sequence[i].freq / (Math.Pow(2, dkoef)));
                freqcoef = summ;
            }
        }
        struct Nscstep
        {
            public SPoint amply, pivot;
            public byte freq;
            public Nscstep(byte freq, SPoint amply, SPoint pivot)
            {
                this.freq = freq; this.amply = amply.Clone(); this.pivot = pivot.Clone();
            }
            public bool Equals(Nscstep obj)
            {
                if (Math.Abs(freq - obj.freq) <= acc_range_freq)
                    if (Math.Abs(amply.x - obj.amply.x) <= acc_range_size && Math.Abs(amply.y - obj.amply.y) <= acc_range_size)
                    {
                        float mpos = (float)Math.Round((double)(pivot.x > obj.pivot.x ? pivot.x / obj.pivot.x : obj.pivot.x / pivot.x), 4);
                        if (mpos == 0) {
                            if (Math.Abs(pivot.x - obj.pivot.x) <= acc_range_pivot)
                            {
                                mpos = (float)Math.Round((double)(pivot.y > obj.pivot.y ? pivot.y / obj.pivot.y : obj.pivot.y / pivot.y), 4);
                                if (mpos == 0) { if (Math.Abs(pivot.y - obj.pivot.y) <= acc_range_pivot) return true; }
                                else if (pivot.y > obj.pivot.y) { if (Math.Abs(pivot.y - obj.pivot.y * mpos) <= acc_range_pivot) return true; }
                                else if (Math.Abs(pivot.y * mpos - obj.pivot.y) <= acc_range_pivot) return true;
                            }
                        } else if (pivot.x > obj.pivot.x) {
                            if (Math.Abs(pivot.x - obj.pivot.x * mpos) <= acc_range_pivot)
                            {
                                mpos = (float)Math.Round((double)(pivot.y > obj.pivot.y ? pivot.y / obj.pivot.y : obj.pivot.y / pivot.y), 4);
                                if (mpos == 0) { if (Math.Abs(pivot.y - obj.pivot.y) <= acc_range_pivot) return true; }
                                else if (pivot.y > obj.pivot.y) { if (Math.Abs(pivot.y - obj.pivot.y * mpos) <= acc_range_pivot) return true; }
                                else if (Math.Abs(pivot.y * mpos - obj.pivot.y) <= acc_range_pivot) return true;
                            }
                        } else {
                            if (Math.Abs(pivot.x * mpos - obj.pivot.x) <= acc_range_pivot)
                            {
                                mpos = (float)Math.Round((double)(pivot.y > obj.pivot.y ? pivot.y / obj.pivot.y : obj.pivot.y / pivot.y), 4);
                                if (mpos == 0) { if (Math.Abs(pivot.y - obj.pivot.y) <= acc_range_pivot) return true; }
                                else if (pivot.y > obj.pivot.y) { if (Math.Abs(pivot.y - obj.pivot.y * mpos) <= acc_range_pivot) return true; }
                                else if (Math.Abs(pivot.y * mpos - obj.pivot.y) <= acc_range_pivot) return true;
                            }
                        }
                    }
                return false;
            }
        }
    }
}
