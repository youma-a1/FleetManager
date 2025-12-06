namespace FleetManager
{
    public static class AppConfig
    {
        // 🔹 MODIFIER ICI AVEC TES VRAIES INFOS
        public static string ConnectionString = "Server=localhost;Database=boutique_vehicules;Uid=root;Pwd=;Port=3306;";

        // Si ton mot de passe MySQL n'est pas vide, change Pwd=; en Pwd=ton_mot_de_passe;

        // Reste du code...
        public static string FtpServer = "ftp.tonserveur.com";
        public static string FtpUsername = "fleet_user";
        public static string FtpPassword = "ton_mot_de_passe";
        public static string FtpImagePath = "/images/vehicules/";
        public static string ImageBaseUrl = "http://tonserveur.com/images/vehicules/";
        public static bool UseLocalStorage = true;
        public static string LocalImagePath = @"C:\FleetManager\Images";
    }
}