using GeoHash.Net.GeoCoords;
using GeoHash.Net.Utilities.Encoders;
using GeoHash.Net.Utilities.Enums;
using GeoHash.Net.Utilities.Matchers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{


    class Program
    {
        //Cambiar las rutas de los archivos
        static readonly string _resultsFile = @"C:\Users\Alvaro\Downloads\results_point.txt";
        static readonly string _textFile = @"C:\Users\Alvaro\Downloads\test_points.txt";
        static void Main(string[] args)
        {
            Stopwatch time = new Stopwatch();
            time.Start();
            if (File.Exists(_textFile))
            {

                //hacer una lista con todos los geohash y otra con las coordenadas
                List<string> hashesList = new List<string>(); ;
                List<string> coordinateList = new List<string>();

                //se borra el archivo de resultados en el caso de que exista
                if (File.Exists(_resultsFile))
                {
                    File.Delete(_resultsFile);
                }

                //obtengo los geoHash completos para cada par de coordenadas
                getGeoHash(_textFile, ref hashesList, ref coordinateList);

                //funcion que se encarga de obtener el prefijo unico
                getPrefijo(ref hashesList, ref coordinateList, _resultsFile);

            }
            else
            {
                Console.WriteLine("No se ha encontrado el archivo\n");
            }
            time.Stop();
            WriteHead(time, _resultsFile);
        }


        // función para añadir al archivo de resultados al inicio el tiempo de computo y la descripción de las columnas
        static void WriteHead(Stopwatch time, string routeFileName)
        {
            string tempfile = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(tempfile))
            using (StreamReader reader = new StreamReader(routeFileName))
            {
                writer.WriteLine(time.Elapsed.TotalSeconds);
                writer.WriteLine("lat,lng,GeoHash,Prefijo");
                while (!reader.EndOfStream)
                    writer.WriteLine(reader.ReadLine());
            }
            File.Copy(tempfile, routeFileName, true);
        }

        static void getGeoHash(string routeFileName, ref List<string> hashesList, ref List<string> coordinateList)
        {
            GeoHashEncoder<string> encoder = new GeoHashEncoder<string>();
            List<string> coordinateListDup = new List<string>();
            double flagText;

            //recojo las coordenadas del archivo en una lista
            using (StreamReader reader = new StreamReader(routeFileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] lat = line.Split(",");
                    if (double.TryParse(lat[0], out flagText))
                    {
                        coordinateListDup.Add(line);
                    }
                }
            }
            //descarto todas las coordenadas duplicadas
            coordinateList = coordinateListDup.Distinct().ToList();

            //transformar cada linea (lt,ld) en un geohash
            //int x = 0;
            foreach (string line in coordinateList)
            {
                string[] lat = line.Split(",");
                //trato la primera linea para que no entren caracteres
                if (double.TryParse(lat[0], out flagText))
                {
                    double latitude = double.Parse(lat[0], CultureInfo.InvariantCulture);
                    double length = double.Parse(lat[1], CultureInfo.InvariantCulture);
                    string geoHash = encoder.Encode(latitude, length);
                    hashesList.Add(geoHash);
                    //x += 1;
                    //Console.WriteLine($"{x} - coordenada {latitude},{length} con geohash {geoHash}");

                }
            }

        }

        static void getPrefijo(ref List<string> hashesList, ref List<string> coordinateList, string routeFileName)
        {
            GeoHashMatcher<string> matcher = new GeoHashMatcher<string>();
            //lista temporal con los mismos geohash que la original
            List<string> tempHashesList = new List<string>(hashesList);

            //int x = 0;

            using (StreamWriter sw = File.CreateText(routeFileName))
            {
                for (int i = 0; i < hashesList.Count; i++)
                {
                    tempHashesList.Remove(hashesList[i]);
                    //logica de sacar el prefijo unico:
                    //esa lista se copia en otra temporal donde se elimina el geohash a buscar y se hace el getmatches con cada elemento de la lista
                    //en el caso de que getmatches de un solo resultado diferente de null nos quedamos con el indice de la precision
                    //se añade el geohash al nuevo archivo y con ese indice se quitan caracteres hasta dejar el geohash minimo y se añade tambien al nuevo archivo
                    for (int j = 0; j < 12; j++)
                    {
                        IEnumerable<string> isMatch = matcher.GetMatches(hashesList[i], tempHashesList, GeoHashPrecision.Level1 + j);
                        //si en la lista no hay elementos se considera que el geoHash ya ha encontrado su prefijo unico
                        if (!isMatch.Any())
                        {
                            j += 1;
                            string uniquePrefix = hashesList[i].Substring(0, j);

                            //se añade al archivo de texto
                            sw.WriteLine(coordinateList[i] + "," + hashesList[i] + "," + uniquePrefix);
                            //x += 1;
                            //Console.WriteLine($"{x} - El prefijo unico del GeoHash {i + 1} es {uniquePrefix}");
                            break;
                        }
                        if (j == 11 && isMatch.Any())
                        {
                            //se añade al archivo de texto
                            sw.WriteLine(coordinateList[i] + "," + hashesList[i] + "," + hashesList[i]);
                            //x += 1;
                            //Console.WriteLine($"{x} - El prefijo unico del GeoHash {i + 1} es {hashesList[i]}");
                        }
                    }
                    //se vuelve a añadir a la lista porque añadir y eliminar un elemento siempre es mas rapido que copiar la lista entera
                    tempHashesList.Add(hashesList[i]);
                }
                //Console.WriteLine($"la lista de hashes tiene {hashesList.Count} la lista de coordenadas tiene {coordinateList.Count} y he cogido {x}");
            }
        }
    }

}

