using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NickBuhro.Translit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace WeatherBot
{
    class Program
    {
        private static string token { get; set; } = "5382872092:AAFqGQ05LsJwydgKZg-yl39qrTlzldEcCi0";
        private static TelegramBotClient client;

        private static string NameCity;
        private static int Period = 1;

        public static void Main(string[] args)
        {
            client = new TelegramBotClient(token) { Timeout = TimeSpan.FromSeconds(10)};

            var me = client.GetMeAsync().Result;
            Console.WriteLine($"Bot_Id: {me.Id} \nBot_Name: {me.FirstName} ");
            
            client.OnMessage += Bot_OnMessage;
            client.StartReceiving();
            Console.ReadLine();
            client.StopReceiving();
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            if (message.Type == MessageType.Text)
            {
                if (int.TryParse(message.Text, out int n))
                {
                    int period = Convert.ToInt32(message.Text);
                    if (period < 1 || period > 7)
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Некорректный период отображения!");
                    }
                    else 
                    {
                        Period = period;
                    }
                }
                else 
                {
                    NameCity = message.Text;
                }

                if (NameCity != null)
                {
                    List<float> forecastList = Weather(NameCity, Period);

                    string weatherInfo = $"Погода в {Transliteration.LatinToCyrillic(NameCity, Language.Russian)}:\n";

                    DateTime date = DateTime.Now;

                    for (int i = 0; i < Period; i++)
                    {
                        weatherInfo += $"{date.Day}/{date.Month}/{date.Year}\n    Температура: {Math.Round(forecastList[i] - 273)}°C\n";
                        date = date.AddDays(1);
                    }

                    await client.SendTextMessageAsync(message.Chat.Id, weatherInfo);
                }

                else 
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Задайте название города!");
                }
            }
        }

        public static List<float> Weather(string cityName, int period)
        {
            try
            {               
                string posDecodeUrl = "http://open.mapquestapi.com/geocoding/v1/address?key=Vs4oje2PlkuqWdDWNJ1F054jVTgAsVRE&location=" + cityName;

                HttpWebRequest httpWebRequestPos = (HttpWebRequest)WebRequest.Create(posDecodeUrl);
                HttpWebResponse httpWebResponsePos = (HttpWebResponse)httpWebRequestPos?.GetResponse();
                string posResponse;

                using (StreamReader streamReader = new StreamReader(httpWebResponsePos.GetResponseStream()))
                {
                    posResponse = streamReader.ReadToEnd();
                }


                JObject locationSearch = JObject.Parse(posResponse);
                Position cityPos = locationSearch["results"][0]["locations"][0]["displayLatLng"].ToObject<Position>();


                string weatherUrl = "https://api.openweathermap.org/data/2.5/onecall?lat=" + cityPos.lat + "&lon=" + cityPos.lng + "&unit=metric&exclude=hourly,minutely&appid=2351aaee5394613fc0d14424239de2bd";

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(weatherUrl);
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest?.GetResponse();
                string response;

                using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                }

                JObject forecastSearch = JObject.Parse(response);
                List<float> forecastList = new List<float>();

                for (int i = 0; i < period; i++) 
                {
                    float dailyForecast = forecastSearch["daily"][i]["temp"]["day"].ToObject<float>();
                    forecastList.Add(dailyForecast);
                }

                return forecastList;

            }
            catch (System.Net.WebException)
            {
                Console.WriteLine("Возникло исключение");
                return null;
            }
        }
    }
}
