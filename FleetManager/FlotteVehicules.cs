using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public class FlotteVehicules
    {
        private List<VehiculeFlotte> vehicules;
        private string connectionString;

        public FlotteVehicules(string connstr)
        {
            vehicules = new List<VehiculeFlotte>();
            connectionString = connStr;
            ChargerVehicules();
        }

        private void ChargerVehicules()
        {
            try
            {
                vehicules.Clear();
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, marque, modele, immatriculation, carburant, kilometrage, statut FROM vehicules";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vehicules.Add(new VehiculeFlotte
                            {
                                Id = reader.GetInt32("id"),
                                Marque = reader.GetString("marque"),
                                Modele = reader.GetString("modele"),
                                Immatriculation = reader.GetString("immatriculation"),
                                Carburant = reader.GetString("carburant"),
                                Kilometrage = reader.GetInt32("kilometrage"),
                                Statut = reader.GetString("statut")
                            });
                        }
                    }
                }
            }
            catch { }
        }

        // =====================================================
        //  🔹 AJOUTER - INSERT SQL PARAMÉTRÉ (EXERCICE)
        // =====================================================
        public bool Ajouter(VehiculeFlotte vehicule)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // INSERT SQL PARAMÉTRÉ
                    string query = @"INSERT INTO vehicules 
                        (marque, modele, immatriculation, carburant, kilometrage, statut, annee, prix, date_ajout)
                        VALUES (@Marque, @Modele, @Immat, @Carburant, @Km, 'actif', 2024, 0, NOW())";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Paramètres pour éviter les injections SQL
                        cmd.Parameters.AddWithValue("@Marque", vehicule.Marque);
                        cmd.Parameters.AddWithValue("@Modele", vehicule.Modele);
                        cmd.Parameters.AddWithValue("@Immat", vehicule.Immatriculation);
                        cmd.Parameters.AddWithValue("@Carburant", vehicule.Carburant);
                        cmd.Parameters.AddWithValue("@Km", vehicule.Kilometrage);

                        cmd.ExecuteNonQuery();
                    }
                }

                ChargerVehicules();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        //  🔹 SUPPRIMER - DELETE SQL (EXERCICE)
        // =====================================================
        public bool Supprimer(string immatriculation)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string queryId = "SELECT id FROM vehicules WHERE immatriculation = @Immat";
                    int vehiculeId = 0;
                    using (var cmd = new MySqlCommand(queryId, conn))
                    {
                        cmd.Parameters.AddWithValue("@Immat", immatriculation);
                        var result = cmd.ExecuteScalar();
                        if (result != null) vehiculeId = Convert.ToInt32(result);
                    }

                    if (vehiculeId > 0)
                    {
                        using (var cmd = new MySqlCommand("DELETE FROM historique_vehicules WHERE vehicule_id = @VId", conn))
                        {
                            cmd.Parameters.AddWithValue("@VId", vehiculeId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new MySqlCommand("DELETE FROM vehicules WHERE immatriculation = @Immat", conn))
                        {
                            cmd.Parameters.AddWithValue("@Immat", immatriculation);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                ChargerVehicules();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        //  🔹 RECHERCHER PAR IMMATRICULATION (EXERCICE)
        // =====================================================
        public VehiculeFlotte RechercherParImmatriculation(string immatriculation)
        {
            ChargerVehicules();
            return vehicules.FirstOrDefault(v => v.Immatriculation.ToUpper() == immatriculation.ToUpper());
        }

        // =====================================================
        //  🔹 RECHERCHER PAR MARQUE (EXERCICE)
        // =====================================================
        public List<VehiculeFlotte> RechercherParMarque(string marque)
        {
            ChargerVehicules();
            return vehicules.Where(v => v.Marque.ToLower().Contains(marque.ToLower())).ToList();
        }

        // =====================================================
        //  🔹 RECHERCHER PAR TYPE D'ÉNERGIE (EXERCICE)
        // =====================================================
        public List<VehiculeFlotte> RechercherParTypeEnergie(string carburant)
        {
            ChargerVehicules();
            return vehicules.Where(v => v.Carburant.ToLower() == carburant.ToLower()).ToList();
        }

        // =====================================================
        //  🔹 RECHERCHER PAR KILOMÉTRAGE MAX (EXERCICE)
        // =====================================================
        public List<VehiculeFlotte> RechercherParKilometrageMax(int kmMax)
        {
            ChargerVehicules();
            return vehicules.Where(v => v.Kilometrage <= kmMax).OrderBy(v => v.Kilometrage).ToList();
        }

        public List<VehiculeFlotte> ObtenirTous()
        {
            ChargerVehicules();
            return vehicules;
        }
    }

    // =====================================================
    //  🔹 CLASSE VEHICULEFLOTTE
    // =====================================================
    public class VehiculeFlotte
    {
        public int Id { get; set; }
        public string Marque { get; set; }
        public string Modele { get; set; }
        public string Immatriculation { get; set; }
        public string Carburant { get; set; }
        public int Kilometrage { get; set; }
        public string Statut { get; set; }
    }
}