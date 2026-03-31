using FleetManager;
using MySql.Data.MySqlClient;
using System.IO;
using System;

namespace FleetManager
{
    public static class AppConfig
    {
        // 🔹 Configuration de la base de données
        public static string ConnectionString { get; } =
            "Server=localhost;Database=boutique_vehicules;Uid=root;Pwd=;";

        /// <summary>
        /// Retourne une nouvelle connexion MySQL
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        // 🔹 Mode de stockage (true = local, false = FTP)
        public static bool UseLocalStorage { get; set; } = true;

        // 🔹 Configuration stockage LOCAL
        public static string LocalImagePath { get; set; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        // 🔹 Configuration FTP (à configurer selon ton hébergeur)
        public static string FtpServer { get; set; } = "ftp://votre-serveur.com";
        public static string FtpUsername { get; set; } = "votre_username";
        public static string FtpPassword { get; set; } = "votre_password";
        public static string FtpImagePath { get; set; } = "images/vehicules";

        /// <summary>
        /// Configure les paramètres FTP
        /// </summary>
        public static void ConfigureFtp(string server, string username, string password, string imagePath = "images/vehicules")
        {
            FtpServer = server;
            FtpUsername = username;
            FtpPassword = password;
            FtpImagePath = imagePath;
            UseLocalStorage = false;
        }

        /// <summary>
        /// Active le stockage local
        /// </summary>
        public static void UseLocalStorageMode(string customPath = null)
        {
            UseLocalStorage = true;
            if (!string.IsNullOrEmpty(customPath))
            {
                LocalImagePath = customPath;
            }
        }

        /// <summary>
        /// Initialise le dossier d'images local
        /// </summary>
        public static void InitializeLocalStorage()
        {
            if (UseLocalStorage && !Directory.Exists(LocalImagePath))
            {
                Directory.CreateDirectory(LocalImagePath);
            }
        }
    }
}
