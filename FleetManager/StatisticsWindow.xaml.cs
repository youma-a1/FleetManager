using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class StatisticsWindow : Window
    {
        private string connectionString;

        public StatisticsWindow(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;
            ShowToday_Click(null, null); // Affiche les stats du jour par défaut
        }

        private void ShowToday_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics("TODAY", "Statistiques du jour");
        }

        private void ShowWeek_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics("WEEK", "Statistiques de la semaine");
        }

        private void ShowMonth_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics("MONTH", "Statistiques du mois");
        }

        private void ShowYear_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics("YEAR", "Statistiques de l'année");
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics("ALL", "Toutes les statistiques");
        }

        private void LoadStatistics(string period, string title)
        {
            PeriodTitle.Text = title;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Construit la condition de date
                    string dateCondition;
                    switch (period)
                    {
                        case "TODAY":
                            dateCondition = "DATE(date_modification) = CURDATE()";
                            break;
                        case "WEEK":
                            dateCondition = "YEARWEEK(date_modification) = YEARWEEK(NOW())";
                            break;
                        case "MONTH":
                            dateCondition = "YEAR(date_modification) = YEAR(NOW()) AND MONTH(date_modification) = MONTH(NOW())";
                            break;
                        case "YEAR":
                            dateCondition = "YEAR(date_modification) = YEAR(NOW())";
                            break;
                        default:
                            dateCondition = "1=1"; // ALL
                            break;
                    }

                    // 🔹 Statistiques globales
                    string statsQuery = $@"
                        SELECT 
                            SUM(CASE WHEN action = 'Création' THEN 1 ELSE 0 END) as vehicules_ajoutes,
                            SUM(CASE WHEN action = 'Modification' THEN 1 ELSE 0 END) as modifications,
                            SUM(CASE WHEN action = 'Mise à jour KM' THEN 1 ELSE 0 END) as maj_km,
                            SUM(CASE WHEN action = 'Mise à jour KM' THEN (nouveau_km - ancien_km) ELSE 0 END) as total_km
                        FROM historique_vehicules
                        WHERE {dateCondition}";

                    using (var cmd = new MySqlCommand(statsQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            VehiclesAddedText.Text = reader.IsDBNull(0) ? "0" : reader.GetInt32(0).ToString();
                            ModificationsText.Text = reader.IsDBNull(1) ? "0" : reader.GetInt32(1).ToString();
                            KmUpdatesText.Text = reader.IsDBNull(2) ? "0" : reader.GetInt32(2).ToString();

                            double totalKm = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                            TotalKmText.Text = $"{totalKm:N0} km";
                        }
                    }

                    // 🔹 Historique détaillé
                    var history = new List<HistoryInfo>();

                    string historyQuery = $@"
                        SELECT 
                            h.date_modification,
                            CONCAT(v.marque, ' ', v.modele, ' (', v.immatriculation, ')') as vehicule,
                            h.action,
                            CONCAT(u.prenom, ' ', u.nom) as agent,
                            h.description
                        FROM historique_vehicules h
                        JOIN vehicules v ON h.vehicule_id = v.id
                        JOIN utilisateurs u ON h.agent_id = u.id
                        WHERE {dateCondition}
                        ORDER BY h.date_modification DESC
                        LIMIT 100";

                    using (var cmd = new MySqlCommand(historyQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new HistoryInfo
                            {
                                Date = reader.GetDateTime(0).ToString("dd/MM/yyyy HH:mm"),
                                Vehicule = reader.GetString(1),
                                Action = reader.GetString(2),
                                Agent = reader.GetString(3),
                                Details = reader.GetString(4)
                            });
                        }
                    }

                    HistoryDataGrid.ItemsSource = history;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des statistiques : {ex.Message}", "Erreur");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class HistoryInfo
    {
        public string Date { get; set; }
        public string Vehicule { get; set; }
        public string Action { get; set; }
        public string Agent { get; set; }
        public string Details { get; set; }
    }
}