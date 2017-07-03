using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ble
{
    class textLogger
    {
        public string logFilename;
        public string logPathname;
        public double expireDays = 10;
        private StreamWriter logger;
        public string logMode = "H"; // D:Daily H:Hourly
        public List<string> writeList = new List<string>();
        public textLogger()
        {

        }

        public bool initLogger()
        {
            //Thread doDeleteThread = new Thread(doDeleteExpiredFile);
            //doDeleteThread.IsBackground = true;
            //doDeleteThread.Start();

            //Thread doWriteLogThread = new Thread(do_write_txt_log);
            //doWriteLogThread.IsBackground = true;
            //doWriteLogThread.Start();

            if (logFilename.Length > 0)
            {
                if (!Directory.Exists(logPathname))
                {
                    Directory.CreateDirectory(logPathname);
                }
                switch (logMode)
                {
                    case "D":
                        logger = new StreamWriter(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd") + "_" + logFilename + ".txt", true);
                        break;
                    case "H":
                        logger = new StreamWriter(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt", true);
                        break;
                    default:
                        break;
                }

                //write_txt_log("---------------------------------------  Logger start !!! -------------------------------------------------");

                return true;
            }
            else
            {
                return false;
            }
        }

        private void doDeleteExpiredFile()
        {
            while (true)
            {
                DateTime theExpireDate = DateTime.Now.AddDays(expireDays * -1);
                string theFileNameToDelete = logPathname + "\\" + theExpireDate.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt";
                switch (logMode)
                {
                    case "D":
                        theFileNameToDelete = logPathname + "\\" + theExpireDate.ToString("yyyy_MM_dd") + "_" + logFilename + ".txt";
                        break;
                    case "H":
                        theFileNameToDelete = logPathname + "\\" + theExpireDate.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt";
                        break;
                    default:
                        break;
                }
                if (File.Exists(theFileNameToDelete))
                {
                    try
                    {
                        File.Delete(theFileNameToDelete);
                        write_txt_log("delete file " + theFileNameToDelete + " OK");
                    }
                    catch (Exception err)
                    {
                        write_txt_log("delete File " + theFileNameToDelete + " Error " + err.ToString());
                    }
                }

                Thread.Sleep(300000);
            }
        }

        public void do_write_txt_log()
        {
            string log_string = "";

            while (true)
            {
                Thread.Sleep(1);
                {
                    Monitor.Enter(writeList);
                    if (writeList.Count > 0)
                    {
                        log_string = writeList[0];
                        writeList.RemoveAt(0);
                        try
                        {
                            switch (logMode)
                            {
                                case "D":
                                    if (!File.Exists(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd") + "_" + logFilename + ".txt"))
                                    {

                                        logger = File.CreateText(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd") + "_" + logFilename + ".txt");

                                    }
                                    break;
                                case "H":
                                    if (!File.Exists(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt"))
                                    {

                                        logger = File.CreateText(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt");

                                    }
                                    break;
                                default:
                                    break;
                            }

                            logger.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " , " + log_string);
                            logger.Flush();//****
                        }
                        catch (Exception err)
                        {


                        }
                    }
                    Monitor.Exit(writeList);
                }
            }

        }

        public void write_txt_log(string log_string)
        {
            try
            {
                switch (logMode)
                {
                    case "D":
                        if (!File.Exists(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd") + "_" + logFilename + ".txt"))
                        {

                            logger = File.CreateText(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd") + "_" + logFilename + ".txt");

                        }
                        break;
                    case "H":
                        if (!File.Exists(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt"))
                        {

                            logger = File.CreateText(logPathname + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH") + "_" + logFilename + ".txt");

                        }
                        break;
                    default:
                        break;
                }

                //logger.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " , " + log_string);
                logger.WriteLine(log_string );
                logger.Flush();//****
            }
            catch (Exception err)
            {


            }

        }
    }
}
