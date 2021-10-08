using System;
using System.Collections.Generic;
using System.IO;

namespace YesNt.Interpreter
{
    public class YesNtInterpreter_OLD
    {
        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();
        private readonly Dictionary<string, int> labels = new Dictionary<string, int>();
        private List<string> lines = new List<string>();
        private Stack<int> lastLabels = new();

        public void Execute(string path)
        {
            lines.Clear();
            variables.Clear();
            labels.Clear();
            lastLabels.Clear();

            LoadFile(path);

            string searchLabel = string.Empty;

            for (int lineNum = 0; lineNum < lines.Count; lineNum++)
            {
                string line = lines[lineNum].Trim().Replace("\r", "");

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (searchLabel == string.Empty)
                {
                    if (line.Contains("%crl"))
                    {
                        line = line.Replace("%crl", ToSaveString(Console.ReadLine()));
                    }
                    if (line.Contains("%cr"))
                    {
                        line = line.Replace("%cr", ToSaveString(Console.ReadKey().KeyChar.ToString()));
                    }

                    foreach (KeyValuePair<string, string> variable in variables)
                    {
                        line = line.Replace($">{variable.Key}", variable.Value);
                    }

                    if (line.Contains(" "))
                    {
                        int index = line.IndexOf(' ');
                        if (line.Contains(" = ") && line.IndexOf('=') > index)
                        {
                            index = line.IndexOf('=') + 1;
                        }

                        string cmd = line.Substring(0, index);
                        cmd += Calculate(line.Substring(index));
                        line = cmd;
                    }
                }

                if (line.StartsWith("%lbl "))
                {
                    string key = line.Substring(5).Trim();
                    if (labels.ContainsKey(key))
                    {
                        labels[key] = lineNum;
                    }
                    else
                    {
                        labels.Add(key, lineNum);
                    }

                    if (searchLabel != string.Empty && searchLabel == key)
                    {
                        searchLabel = string.Empty;
                    }
                    continue;
                }

                if (searchLabel != string.Empty)
                {
                    continue;
                }

                if (line.StartsWith("%imp "))
                {
                    string pat = line.Substring(5).Trim();
                    lines.RemoveAt(lineNum);
                    LoadFile(pat, lineNum);
                    lineNum--;
                }
                else if (line.StartsWith("%jmp "))
                {
                    string key = FromSaveString(line.Substring(5).Trim());
                    lastLabels.Push(lineNum);

                    if (labels.ContainsKey(key))
                    {
                        lineNum = labels[key];
                    }
                    else
                    {
                        searchLabel = key;
                    }
                }
                else if (line.StartsWith("%jif "))
                {
                    if (line.Contains("|"))
                    {
                        string dat = line.Substring(5).Trim();
                        string key = dat.Substring(0, dat.IndexOf('|')).Trim();
                        string condition = dat.Substring(dat.IndexOf('|')).Replace("|", "").Trim();
                        if (EvaluateCondition(condition))
                        {
                            lastLabels.Push(lineNum);
                            if (labels.ContainsKey(key))
                            {
                                lineNum = labels[key];
                            }
                            else
                            {
                                searchLabel = key;
                            }
                        }
                    }
                }
                else if (line.Equals("%ret"))
                {
                    lineNum = lastLabels.Pop();
                }
                else if (line.Equals("%end"))
                {
                    break;
                }
                else if (line.StartsWith("%cwl "))
                {
                    Console.WriteLine(FromSaveString(line.Substring(5)));
                }
                else if (line.StartsWith("%cw "))
                {
                    Console.Write(FromSaveString(line.Substring(4)));
                }
                else if (line.StartsWith("<"))
                {
                    string[] parts = line.Split(" = ");
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Replace("<", "");
                        if (variables.ContainsKey(key))
                        {
                            variables[key] = parts[1];
                        }
                        else
                        {
                            variables.Add(key, parts[1]);
                        }
                    }
                }
            }
        }

        private void LoadFile(string path, int index = 0)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File \"{path}\" not found!");
                return;
            }

            string input = File.ReadAllText(path);
            string[] newLines = input.Split("\n");
            foreach (string line in newLines)
            {
                lines.Insert(index, line);
                index++;
            }
        }

        private string ToSaveString(string input)
        {
            string output = "";
            foreach (char c in input)
            {
                output += $"\r{c}\r";
            }
            return output;
        }

        private string FromSaveString(string input)
        {
            return input.Replace("\r", "");
        }

        private bool EvaluateCondition(string input)
        {
            string[] parts = input.Split(" == ");
            if (parts.Length == 2)
            {
                string part1 = FromSaveString(parts[0]);
                string part2 = FromSaveString(parts[1]);
                return part1 == part2;
            }

            parts = input.Split(" != ");
            if (parts.Length == 2)
            {
                string part1 = FromSaveString(parts[0]);
                string part2 = FromSaveString(parts[1]);
                return part1 != part2;
            }

            parts = input.Split(" > ");
            if (parts.Length == 2)
            {
                bool succ1 = double.TryParse(FromSaveString(parts[0]), out double part1);
                bool succ2 = double.TryParse(FromSaveString(parts[1]), out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 > part2;
            }

            parts = input.Split(" < ");
            if (parts.Length == 2)
            {
                bool succ1 = double.TryParse(FromSaveString(parts[0]), out double part1);
                bool succ2 = double.TryParse(FromSaveString(parts[1]), out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 < part2;
            }

            parts = input.Split(" >= ");
            if (parts.Length == 2)
            {
                bool succ1 = double.TryParse(FromSaveString(parts[0]), out double part1);
                bool succ2 = double.TryParse(FromSaveString(parts[1]), out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 >= part2;
            }

            parts = input.Split(" <= ");
            if (parts.Length == 2)
            {
                bool succ1 = double.TryParse(FromSaveString(parts[0]), out double part1);
                bool succ2 = double.TryParse(FromSaveString(parts[1]), out double part2);
                if (!succ1 || !succ2)
                {
                    return false;
                }

                return part1 <= part2;
            }

            return false;
        }

        private string Calculate(string input, char op = '+')
        {
            string[] parts = input.Split($" {op} ");
            string result = "";

            double number = double.NaN;
            int spacesToAdd = 0;

            foreach (string p in parts)
            {
                string part = p;

                switch (op)
                {
                    case '+':
                        part = Calculate(part, '-');
                        break;

                    case '-':
                        part = Calculate(part, '*');
                        break;

                    case '*':
                        part = Calculate(part, '/');
                        break;
                }
                if (double.TryParse(FromSaveString(part), out double num))
                {
                    if (double.IsNaN(number))
                    {
                        spacesToAdd = CountSpaces(part);
                        number = num;
                    }
                    else
                    {
                        switch (op)
                        {
                            case '+':
                                number += num;
                                break;

                            case '-':
                                number -= num;
                                break;

                            case '*':
                                number *= num;
                                break;

                            case '/':
                                number /= num;
                                break;
                        }
                    }
                }
                else
                {
                    if (!double.IsNaN(number))
                    {
                        result += new string(' ', spacesToAdd) + number;
                    }

                    result += part;

                    number = double.NaN;
                }
            }

            if (!double.IsNaN(number))
            {
                result += new string(' ', spacesToAdd) + number;
            }

            return result;
        }

        private int CountSpaces(string input)
        {
            int count = 0;
            while (input[count] == ' ')
            {
                count++;
            }

            return count;
        }
    }
}