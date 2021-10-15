using System;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace VatApp
{
    public class VatApp
    {
        // Start variables
        public static string
        mConnectionString = null,
        mDate = GetCurrentDateInSpecifiedFormat(false),
        mDateShort = GetCurrentDateInSpecifiedFormat(true);

        public static DateTime mDateTime = DateTime.Now;
        public static string[] mConfig = new string[5];
        public static HttpClient mHttpClient = new HttpClient();
        // End variables


        /// <summary>
        /// Function that read config from file.
        /// </summary>
        /// <param name="fileName">Path to file</param>
        protected static void ReadConfig(string fileName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    mConfig[0] = sr.ReadLine();  // Godzina odpalenia aplikacji
                    mConfig[1] = sr.ReadLine();  // Ścieżka do folderu z plikami
                    mConfig[2] = sr.ReadLine();  // Connection string
                    mConfig[3] = sr.ReadLine();  // Nazwa tabeli output
                    mConfig[4] = sr.ReadLine();  // Nazwa tabeli viewsa
                }
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message, "Błąd!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Function that starts checking contractors statuses
        /// </summary>
        public static void StartCheckingStatus()


        {
            File.WriteAllText("hash.txt", Sha512_FromString("test"));

            ReadConfig("config.txt");
            mConnectionString = mConfig[2];
            DownloadFlat(mDateShort);
            mHttpClient.BaseAddress = new Uri("https://wl-api.mf.gov.pl");

            try
            {
                List<string> nips = new List<string>();
                string queryString = $"SELECT NIP FROM {mConfig[4]} WHERE NrRachunku = '';";
                string queryString1 = $"SELECT NIP,NrRachunku FROM {mConfig[4]} WHERE NrRachunku <> '';"; // <-- Skąd ma pobierać numery NIP/NRB

                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(queryString, connection);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (IsValidNip(reader.GetString(0)))
                            {
                                nips.Add(reader.GetString(0));
                            }
                        }
                        reader.Close();
                        CheckContractorsStatuses(String.Join(",", nips));
                    }
                    command.Dispose();

                    SqlCommand command1 = new SqlCommand(queryString1, connection);
                    using (SqlDataReader reader = command1.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CheckContractorStatus(reader[0].ToString(), reader[1].ToString());
                        }
                        reader.Close();
                    }
                    command1.Dispose();

                    connection.Close();
                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Wystąpił błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Environment.Exit(1);
        }

        /// <summary>
        /// Function inserting contractors VAT status to database.
        /// </summary>
        /// <param name="nip">Contractors NIP</param>
        /// <param name="nrb">Contractors Bank Account Number if exists</param>
        /// <param name="statusVat">Contractors status field value in database</param>
        /// <param name="status2">Contractors status2 field value in database</param>
        protected static void InsertStatus(string nip, string nrb, string statusVat, bool status2)
        {
            if (status2)
            {
                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    string query = $"INSERT INTO {mConfig[3]} (NIP,Rachunek,Status2,DataPrzetworzenia) VALUES (@nip,@nrb,@statusVat,@date)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@nip", nip);
                        command.Parameters.AddWithValue("@nrb", nrb);
                        command.Parameters.AddWithValue("@statusVat", statusVat);
                        command.Parameters.AddWithValue("@date", mDate);

                        connection.Open();
                        int result = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    string query = $"INSERT INTO {mConfig[3]} (NIP,Rachunek,Status1,DataPrzetworzenia) VALUES (@nip,@nrb,@statusVat,@date)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@nip", nip);
                        command.Parameters.AddWithValue("@nrb", nrb);
                        command.Parameters.AddWithValue("@statusVat", statusVat);
                        command.Parameters.AddWithValue("@date", mDate);

                        connection.Open();
                        int result = command.ExecuteNonQuery();
                    }
                }
            }

        }

        /// <summary>
        /// Function thats updating exsisting contrators VAT status.
        /// </summary>
        /// <param name="nip">Contractors NIP</param>
        /// <param name="status">Contractors status field value in database</param>
        /// <param name="ver2">If true status1 and status2 fields values are updated</param>
        protected static void UpdateStatus(string nip, string status, bool ver2)
        {
            if (!ver2)
            {
                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    string query = $"UPDATE {mConfig[3]} SET Status1='{status}', Status2='SPRAWDZONY' WHERE Nip='{nip}' AND DataPrzetworzenia='{mDate}';";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        int result = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    string query = $"UPDATE {mConfig[3]} SET Status2='SPRAWDZONY' WHERE Nip='{nip}' AND DataPrzetworzenia='{mDate}';";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        int result = command.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Function downloading flat file from given date.
        /// </summary>
        /// <param name="date">Date from witch flat file need to be downloaded</param>
        protected static void DownloadFlat(string date)
        {
            if (!Directory.Exists(date))
            {
                Form2 form2 = new Form2(@"https://plikplaski.mf.gov.pl/pliki/", $"{date}.7z");
                form2.ShowDialog();
            }
        }

        /// <summary>
        /// Function that calculate Sha512 hash iterated 5000 times from given string.
        /// </summary>
        /// <param name="input">String to be hashed</param>
        /// <returns></returns>
        protected static string Sha512_FromString(string input)
        {
            var sha = new SHA512Managed();
            var _input = Encoding.ASCII.GetBytes(input);
            var output = _input;
            for (int i = 0; i < 5000; i++)
            {
                output = sha.ComputeHash(_input);
                _input = Encoding.ASCII.GetBytes(BitConverter.ToString(output).Replace("-", "").ToLower());
            }

            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Function returning true if NIP is valid.
        /// </summary>
        /// <param name="_nip">NIP</param>
        /// <returns></returns>
        protected static bool IsValidNip(string _nip)
        {
            try
            {
                int[] waga = { 6, 5, 7, 2, 3, 4, 5, 6, 7 }, nip = new int[10];
                int suma = 0;

                if (nip.Length != 10)
                {
                    return false;
                }

                // Suma kontrolna
                for (int i = 0; i < 10; i++)
                    nip[i] = int.Parse(_nip[i].ToString());

                for (int k = 0; k < 9; k++)
                    suma += nip[k] * waga[k];

                if ((suma % 11) == int.Parse(_nip[_nip.Length - 1].ToString()))
                    return true;

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Function returning current date format depending on parameter 'shortDate'
        /// </summary>
        /// <param name="shortDate">Parameter determining format</param>
        /// <returns></returns>
        protected static string GetCurrentDateInSpecifiedFormat(bool shortDate)
        {
            DateTime data = mDateTime;

            string rok = data.Year.ToString(), miesiac, dzien;

            if (data.Month < 10) { miesiac = "0" + data.Month.ToString(); }
            else { miesiac = data.Month.ToString(); }

            if (data.Day < 10)
            {
                dzien = "0" + data.Day.ToString();
            }
            else
            {
                dzien = data.Day.ToString();
            }

            string _date = $"{rok}-{miesiac}-{dzien}";

            if (shortDate)
                return _date.Replace("-", "");
            else
                return _date;
        }

        /// <summary>
        /// Function that checks contractor VAT status in JSON flat file.
        /// </summary>
        /// <param name="nip">Contractors NIP</param>
        /// <param name="nrb">Contractors Bank Account Number</param>
        protected static void CheckContractorStatus(string nip, string nrb)
        {
            string json = File.ReadAllText(Environment.CurrentDirectory + $@"\{mDateShort}\{mDateShort}.json");
            string search = Sha512_FromString(mDateShort + nip + nrb);

            var token = JToken.Parse(json);

            int found = 0;
            int spcCount = token.Value<JArray>("skrotyPodatnikowCzynnych").Count;
            int spzCount = token.Value<JArray>("skrotyPodatnikowZwolnionych").Count;

            // Czynni
            for (int i = 0; i < spcCount; i++)
            {
                if (search == (string)token.Value<JArray>("skrotyPodatnikowCzynnych")[i])
                {
                    InsertStatus(nip, nrb, "Aktywny", false);
                    found = 1;
                }
            }

            // Zwolnieni
            for (int j = 0; j < spzCount; j++)
            {
                if (search == (string)token.Value<JArray>("skrotyPodatnikowZwolnionych")[j])
                {
                    InsertStatus(nip, nrb, "Zwolniony", false);
                    found = 1;
                }
            }

            if (found == 0)
            {
                InsertStatus(nip, nrb, "Nie figuruje", false);
            }

        }

        /// <summary>
        /// Function that checks contractors VAT statuses from API.
        /// </summary>
        /// <param name="nips">NIP numbers splited by ','</param>
        protected static async void CheckContractorsStatuses(string nips)
        {
            string[] nipsArray = nips.Split(',');
            int x = -1, k = nipsArray.Length, l = 0;

            if (k % 30 == 0)
            {
                for (int j = 0; j < k / 30; j++)
                {
                    l++;
                    string _nips = null;

                    if (l <= 10)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            x++;
                            _nips += nipsArray[x] + ",";
                        }

                        await GetStatusFromAPI(_nips.Remove(_nips.Length - 1, 1));
                    }

                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                for (int j = 0; j < k / 30 + 1; j++)
                {
                    l++;
                    string _nips = null;

                    if (l <= 10)
                    {
                        if (j == k / 30)
                        {
                            for (int i = 0; i < k % 30; i++)
                            {
                                x++;
                                _nips += nipsArray[x] + ",";
                            }

                            await GetStatusFromAPI(_nips.Remove(_nips.Length - 1, 1));
                        }

                        else
                        {
                            for (int i = 0; i < 30; i++)
                            {
                                x++;
                                _nips += nipsArray[x] + ",";
                            }

                            await GetStatusFromAPI(_nips.Remove(_nips.Length - 1, 1));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Function that checks contractors VAT statuses from API.
        /// </summary>
        /// <param name="nips">NIP numbers splited by ',' MAX(30)</param>
        /// <returns></returns>
        protected static async Task GetStatusFromAPI(string nips)
        {
            string GetString = string.Empty;
            try
            {
                var Getresult = await mHttpClient.GetAsync($"api/search/nips/{nips}?date={mDate}");
                GetString = await Getresult.Content.ReadAsStringAsync();

                var token = JToken.Parse(GetString);
                var count = token.SelectTokens("$.result.subjects[*]").Count();

                //using (StreamWriter file = new StreamWriter(@"logs.log", true))
                //{
                //    file.WriteLine($"[{mDate}]{Environment.NewLine}HTTP Status: {Getresult.StatusCode.ToString()}");
                //}

                if (Getresult.IsSuccessStatusCode)
                {
                    for (int x = 0; x < nips.Split(',').Length; x++)
                    {
                        InsertStatus(nips.Split(',')[x], "BRAK", "DO SPRAWDZENIA", true);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        UpdateStatus((string)token["result"]["subjects"][i]["nip"], (string)token["result"]["subjects"][i]["statusVat"], false);
                    }

                    for (int k = 0; k < nips.Split(',').Length; k++)
                    {
                        UpdateStatus(nips.Split(',')[k], null, true);
                    }
                }

                else
                {
                    using (StreamWriter file = new StreamWriter(@"logs.log", true))
                    {
                        file.WriteLine($"HTTP Content: {GetString}{Environment.NewLine}");
                    }
                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
