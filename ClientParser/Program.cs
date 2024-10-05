using System.Xml;

public class Program
{
    public class Client
    {
        public Client(string FIO, long RegNumber, string DiasoftID, string Registrator) 
        {
            this.FIO = FIO;
            this.RegNumber = RegNumber;
            this.DiasoftID = DiasoftID;
            this.Registrator = Registrator;
        }

        public string FIO { get; init; }
        public long RegNumber { get; init; }
        public string DiasoftID { get; init; }
        public string Registrator { get; init; }
    }

    class XMLParser
    {
        const string MSG_ErrDiasoftID = "Не указан DiasoftID: "; // Hardcode, may be better add to res
        const string MSG_ErrRegistrator = "Не указан Регистратор: ";
        const string MSG_ErrFIO = "Не указано ФИО: ";
        const string MSG_ErrALL = "Всего ошибочных записей: ";
        string filename;
        int CountErDiasoftID = 0;
        int CountErRegistrator = 0;
        int CountErFIO = 0;
        List<Client> Clients = new List<Client>();
        Dictionary<string, int> Registrators = new Dictionary<string, int>();

        public XMLParser(string filename, DebugLevel dL = DebugLevel.MEDIUM) 
        { 
            this.filename = filename;  
            _debugLevel = dL;
        }
        public enum DebugLevel : byte
        {
            LOW = 0,
            MEDIUM = 1
        }
        private DebugLevel _debugLevel;
        private void DebugWrite(string debugInfo)
        {
            Console.WriteLine(debugInfo);
        }

        public void Parse()
        {
            CountErDiasoftID = 0;
            CountErRegistrator = 0;
            CountErFIO = 0;
            Clients.Clear();
            using (XmlReader reader = XmlReader.Create(filename))
            {
                int RegistratorID = 0;
                while (reader.Read())
                {
                    bool IsBadClient = false;
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Client")
                    {
                        string FIO = "";
                        long RegNumber = -1;
                        string DiasoftID = "";
                        string Registrator = "";
                        
                        if (_debugLevel > DebugLevel.LOW)
                        {
                            int line = (reader is IXmlLineInfo xmlLine && xmlLine.HasLineInfo()) ? xmlLine.LineNumber : -1;
                            DebugWrite("Обработано строк: " + line);
                        }
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "FIO")
                                FIO = reader.ReadElementContentAsString();
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "RegNumber")
                                RegNumber = reader.ReadElementContentAsLong();
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "DiasoftID")
                                DiasoftID = reader.ReadElementContentAsString();
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Registrator")
                            {
                                Registrator = reader.ReadElementContentAsString();
                                if(Registrator != "")
                                    if(Registrators.TryAdd(Registrator, RegistratorID))
                                        RegistratorID++;
                            }
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Client")
                                break;
                        }
                        if(FIO.Length == 0)
                        {
                            CountErFIO++;
                            IsBadClient = true;
                        }
                        if (DiasoftID.Length == 0)
                        {
                            CountErDiasoftID++;
                            IsBadClient = true;
                        }
                        if (Registrator.Length == 0)
                        {
                            CountErRegistrator++;
                            IsBadClient = true;
                        }

                        if (!IsBadClient)
                            Clients.Add(new Client(FIO, RegNumber, DiasoftID, Registrator));


                    }
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Clients")
                        break;
                }
            }
        }

        private void CreateOutputIfNotExist()
        {
            try
            {
                if (!Directory.Exists("Output"))
                    Directory.CreateDirectory("Output");
            }
            catch (Exception ex)
            {
                DebugWrite(ex.Message);
            }
        }

        public void SaveResultXml(string filename)
        {
            CreateOutputIfNotExist();
            using (XmlWriter xmlWriter = XmlWriter.Create($"Output\\ {filename}", new XmlWriterSettings() {OmitXmlDeclaration = true, Indent = true }))
            {

                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Clients");
                foreach (Client c in Clients)
                {
                    xmlWriter.WriteStartElement("Client");
                    xmlWriter.WriteAttributeString("RegistratorID", Registrators[c.Registrator].ToString());
                    xmlWriter.WriteStartElement("FIO");
                    xmlWriter.WriteString(c.FIO);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteStartElement("RegNumber");
                    xmlWriter.WriteValue(c.RegNumber);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteStartElement("DiasoftID");
                    xmlWriter.WriteString(c.DiasoftID);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteStartElement("Registrator");
                    xmlWriter.WriteString(c.Registrator);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        public void SaveRegistratorXml(string filename)
        {
            CreateOutputIfNotExist();
            var sortedDict = from entry in Registrators orderby entry.Value ascending select entry;
            using (XmlWriter xmlWriter = XmlWriter.Create($"Output\\ {filename}", new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true }))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Registrators");
                foreach (var Registrator in sortedDict)
                {
                    xmlWriter.WriteStartElement("Registrator");
                    xmlWriter.WriteStartElement("Name");
                    xmlWriter.WriteString(Registrator.Key);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteStartElement("ID");
                    xmlWriter.WriteValue(Registrator.Value);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        public void SaveErrorsFile(string filename)
        {
            CreateOutputIfNotExist();
            using (StreamWriter sW = new StreamWriter($"Output\\ {filename}"))
            {

                SortedList<int, string> errors = new SortedList<int, string>(new InvertedComparer())
                {
                    {CountErFIO, MSG_ErrFIO},
                    {CountErDiasoftID, MSG_ErrDiasoftID},
                    {CountErRegistrator, MSG_ErrRegistrator}
                };
                
                foreach (var error in errors)
                {
                    sW.WriteLine(error.Value+error.Key);
                    if (_debugLevel > DebugLevel.LOW)
                    {
                        DebugWrite(error.Value + error.Key);
                    }
                }
                sW.WriteLine(MSG_ErrALL + (CountErFIO+ CountErDiasoftID+ CountErRegistrator));
                if (_debugLevel > DebugLevel.LOW)
                {
                    DebugWrite(MSG_ErrALL + (CountErFIO + CountErDiasoftID + CountErRegistrator));
                }
            }
        }
        private class InvertedComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }
    }


    static void Main(string[] args)
    {
        string filename = "";
        if (args.Count() == 0)
        {

            Console.WriteLine("Type name of xml file: ");
            filename = Console.ReadLine() ?? "";
        }
        else // Drag'n'Drop or path from cmd
        {
            filename = args[0];
        }
        if (!File.Exists(filename))
        {
            Console.WriteLine($"{filename} does not exist");
            Console.WriteLine("Type any key to exit");
            Console.ReadKey();
            return;
        }
        XMLParser xmlParser = new XMLParser("Clients.xml");
        xmlParser.Parse();
        xmlParser.SaveResultXml("ResultClients.xml");
        xmlParser.SaveRegistratorXml("Registrators.xml");
        xmlParser.SaveErrorsFile("Errors.txt");
        Console.WriteLine("Type any key to exit");
        Console.ReadKey();
    }
}