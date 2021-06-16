﻿using ImageMagick;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace QuotesExAristotelis_bot
{
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static string _imagePath;
        private static string _command = "/quote";
        private static int _maxChars = 190;
        private static bool _IsWorking = true;
        private static Collection<string> _names = new Collection<string>()
        { "Алкмеон Кротонский", "Анаксарх", "Анахарсис", "Андрокид", "Аристоксен", "Аристотель",
            "Аркесилай", "Архит Тарентский", "Асклепиад из Флиунта", "Гермипп", "Гермоген", "Гестией Перинфский",
            "Гиппас из Метапонта", "Гиппий Элидский", "Горгий", "Дамон из Афин", "Дикеарх",
            "Дионисий Кассий Лонгин", "Диотима", "Евдем Родосский", "Евдокс Книдский",
            "Евклид из Мегары", "Исократ", "Калликл", "Кебет", "Клеарх", "Кратил",
            "Ксениад", "Ксенократ Халкидонский", "Ксенофан", "Левкипп", "Метродор из Лампсака",
            "Метродор Хиосский", "Навсифан", "Платон", "Поликрат", "Продик", "Протагор", "Псевдо-Лонгин",
            "Симмий", "Сократ", "Спевсипп", "Терпсион", "Тисий", "Троил", "Фаний Эресский", "Федон из Элиды",
            "Филолай", "Флавий Тавр Селевк Кир", "Фрасимах", "Эмпедокл", "Эпихарм", "Эсхин из Сфетта", "Эхекрат"
        };

        public static async Task Main()
        {
            if (String.IsNullOrEmpty(Configuration.BotToken))
            {
                Console.WriteLine($"[E] Environment variable TOKEN not set. Exit.");
                Environment.Exit(2);
            }

            Bot = new TelegramBotClient(Configuration.BotToken);

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            SetupTempFolder();

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            AssemblyLoadContext.Default.Unloading += MethodInvokedOnSigTerm;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"[S] Start listening for @{me.Username}");

            while (_IsWorking)
            {
                await Task.Delay(1000);
            }
        }

        private static void MethodInvokedOnSigTerm(AssemblyLoadContext obj)
        {
            Console.WriteLine("[T] SigTerm handled");
            _IsWorking = false;
            Bot.StopReceiving();
            Console.WriteLine("[T] Stop listening, exit");
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            LogMessage(message);

            if (message == null || message.Type != MessageType.Text)
                return;

            string[] parts = message.Text.Split(' ');

            if (parts.Length < 2)
            {
                await Usage(message);
                return;
            }

            string recivedCommand = parts[0];
            string recivedText = message.Text.Substring(parts[0].Length + 1);

            if (recivedCommand.IndexOf("@") != -1)
            {
                recivedCommand = recivedCommand.Substring(0, recivedCommand.IndexOf("@"));
            }

            if (recivedCommand == _command)
            {
                await SendDocument(message, recivedText);
            }

            static async Task SendDocument(Message message, string text)
            {
                DateTime start = DateTime.Now;

                if (text.Length <= _maxChars)
                {
                    text = $"«{UppercaseFirstLetter(text)}»";
                    string file = CreateNewFileName();

                    using (MagickImage image = new MagickImage(new MagickColor("white"), 500, 500))
                    {
                        MagickReadSettings readSettingsText = CreateTextSetings();
                        MagickReadSettings readSettingsSignature = CreateSignatureSettings();

                        using (var caption = new MagickImage($"caption:{text}", readSettingsText))
                        {
                            image.Composite(caption, 50, 50, CompositeOperator.Over);
                        }

                        using (var caption = new MagickImage($"caption:{GetRandomPhilosopherName()}", readSettingsSignature))
                        {
                            image.Composite(caption, 50, 400, CompositeOperator.Over);
                        }

                        image.Write(file);
                    }

                    DateTime end = DateTime.Now;

                    var interval = end - start;

                    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                    using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var fileName = file.Split(Path.DirectorySeparatorChar).Last();
                    await Bot.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: new InputOnlineFile(fileStream, file)
                    );

                    LogPicture(message, interval, fileName);
                }
                else
                {
                    string name;

                    if (String.IsNullOrEmpty(message.From.Username)) name = $"{message.From.FirstName} {message.From.LastName}";
                    else name = message.From.Username;

                    await Bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Эй, {name}, многовато букв ({text.Length}), а можно всего жалких {_maxChars}!");

                    Console.WriteLine($"[E] Too many chars from {message.From}");
                }
            }

            static async Task Usage(Message message)
            {
                string usage = $"Делать так: {_command} + текст, но не больше {_maxChars} символов.";
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );

                Console.WriteLine($"[H] Sent help to {message.From}");
            }
        }

        private static MagickReadSettings CreateSignatureSettings()
        {
            var readSettingsSignature = new MagickReadSettings
            {
                Font = "font/Bitter-LightItalic.ttf",
                FontFamily = "Bitter",
                FontPointsize = 25,
                TextGravity = Gravity.Southeast,
                BackgroundColor = MagickColors.Transparent,
                Height = 50, // height of text box
                Width = 400, // width of text box
            };
            return readSettingsSignature;
        }

        private static MagickReadSettings CreateTextSetings()
        {
            var readSettingsText = new MagickReadSettings
            {
                Font = "font/Bitter-Light.ttf",
                FontFamily = "Bitter",
                TextGravity = Gravity.Center,
                BackgroundColor = MagickColors.Transparent,
                Height = 350, // height of text box
                Width = 400, // width of text box
            };
            return readSettingsText;
        }

        private static void LogPicture(Message message, TimeSpan interval, string fileName)
        {
            Console.WriteLine($"[P] Picture build time = {interval.TotalSeconds.ToString()}");
            if (!String.IsNullOrEmpty(message.Chat.Title)) Console.WriteLine($"[P] Send pictures {fileName} to «{message.Chat.Title}»/{message.From}");
            else Console.WriteLine($"[P] Send pictures {fileName} to {message.From}");
        }

        private static void LogMessage(Message message)
        {
            if (!String.IsNullOrEmpty(message.Chat.Title)) Console.WriteLine($"[M] Message from «{message.Chat.Title}»/{message.From}: {message.Text}");
            else Console.WriteLine($"[M] Message from {message.From}: {message.Text}");
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("[E] Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }

        private static string GetRandomPhilosopherName()
        {
            var rand = new Random();
            int index = rand.Next(_names.Count);

            return _names[index];
        }

        private static void SetupTempFolder()
        {
            _imagePath = Path.Combine(Path.GetTempPath(), "QuotesExAristotelis_bot");

            if (!Directory.Exists(_imagePath)) Directory.CreateDirectory(_imagePath);
            else
            {
                foreach (var file in Directory.GetFiles(_imagePath))
                {
                    System.IO.File.Delete(file);
                }
            }
        }

        private static string CreateNewFileName()
        {
            Guid guid = Guid.NewGuid();
            return Path.Combine(_imagePath, $"{guid}.jpg");
        }

        public static string UppercaseFirstLetter(this string value)
        {
            if (value.Length > 0)
            {
                char[] array = value.ToCharArray();
                array[0] = char.ToUpper(array[0]);
                return new string(array);
            }
            return value;
        }
    }
}