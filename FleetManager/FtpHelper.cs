using System;
using System.IO;
using System.Net;

namespace FleetManager.Helpers
{
    public class FtpHelper
    {
        private string ftpServer;
        private string ftpUsername;
        private string ftpPassword;
        private string ftpImagePath;

        public FtpHelper(string server, string username, string password, string imagePath)
        {
            ftpServer = server;
            ftpUsername = username;
            ftpPassword = password;
            ftpImagePath = imagePath;
        }

        /// <summary>
        /// Upload une image sur le serveur FTP
        /// </summary>
        /// <param name="localFilePath">Chemin local du fichier</param>
        /// <param name="fileName">Nom du fichier à uploader</param>
        /// <returns>URL publique de l'image uploadée</returns>
        public string UploadImage(string localFilePath, string fileName)
        {
            try
            {
                // Vérifie que le fichier existe
                if (!File.Exists(localFilePath))
                {
                    throw new FileNotFoundException("Le fichier source n'existe pas.", localFilePath);
                }

                // Construit l'URL FTP complète
                string ftpFullPath = CombineFtpPath(ftpServer, ftpImagePath, fileName);

                // Crée la requête FTP
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFullPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                // Lit le fichier local
                byte[] fileContents = File.ReadAllBytes(localFilePath);
                request.ContentLength = fileContents.Length;

                // Upload le fichier
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                // Vérifie la réponse
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == FtpStatusCode.ClosingData ||
                        response.StatusCode == FtpStatusCode.FileActionOK)
                    {
                        // Retourne l'URL publique de l'image (HTTP)
                        return ConvertFtpToHttpUrl(ftpFullPath);
                    }

                    throw new Exception($"Échec de l'upload FTP. Code: {response.StatusCode}");
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response is FtpWebResponse response)
                {
                    throw new Exception($"Erreur FTP {response.StatusCode}: {response.StatusDescription}");
                }
                throw new Exception($"Erreur réseau FTP : {webEx.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'upload FTP : {ex.Message}");
            }
        }

        /// <summary>
        /// Supprime une image du serveur FTP
        /// </summary>
        /// <param name="fileName">Nom du fichier à supprimer</param>
        public void DeleteImage(string fileName)
        {
            try
            {
                string ftpFullPath = CombineFtpPath(ftpServer, ftpImagePath, fileName);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFullPath);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Fichier supprimé
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la suppression FTP : {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si un fichier existe sur le serveur FTP
        /// </summary>
        public bool FileExists(string fileName)
        {
            try
            {
                string ftpFullPath = CombineFtpPath(ftpServer, ftpImagePath, fileName);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFullPath);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        /// <summary>
        /// Combine les parties du chemin FTP
        /// </summary>
        private string CombineFtpPath(string server, string path, string fileName)
        {
            // Nettoie le serveur
            server = server.TrimEnd('/');

            // Nettoie le chemin
            path = path.Trim('/');
            if (!string.IsNullOrEmpty(path))
            {
                path = "/" + path + "/";
            }
            else
            {
                path = "/";
            }

            return server + path + fileName;
        }

        /// <summary>
        /// Convertit une URL FTP en URL HTTP pour l'affichage public
        /// </summary>
        private string ConvertFtpToHttpUrl(string ftpUrl)
        {
            return ftpUrl.Replace("ftp://", "http://");
        }
    }
}