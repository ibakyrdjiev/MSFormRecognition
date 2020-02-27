using CustomVision.Enums;
using CustomVision.Utils;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CustomVision
{
    class Program
    {
        const string CUSTOM_VISION_TRAINING_KEY = "9506a73df77b4e88be20dd7a355bb4d2";
        const string CUSTOM_VISION_PREDICTION_KEY = "c522f37502854d9887d188795d16896d";
        const string CUSTOM_VISION_RESOURCE_ID = "/subscriptions/b0e57d14-9b06-4922-bd9d-7881f975b398/resourceGroups/of_first_attempt_resource_group/providers/Microsoft.CognitiveServices/accounts/ja-first-demo-Prediction";
        const string CUSTOM_VISION_ENDPOINT = "https://southcentralus.api.cognitive.microsoft.com/";

        const string PROJECT_TYPE = "ObjectDetection";
        const int BATCH_SIZE = 25;

        const string IMG_FILE_EXTENSION = ".jpg";
        const string IMG_GRID_FILE_NAME_MARK = "_matrix_";
        const char IMG_GRID_FILE_NAME_PARAM_SPLITTER = 'x';

        static void Main(string[] args)
        {
            const string Project_Name = "FA EE First Demo New Test";
            const string Published_Model_Name = "JAEEDemo";

            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = CUSTOM_VISION_TRAINING_KEY,
                Endpoint = CUSTOM_VISION_ENDPOINT
            };

            // <snippet_create>
            // Find the object detection domain
            var domains = trainingApi.GetDomains();
            var objDetectionDomain = domains.FirstOrDefault(d => d.Type == PROJECT_TYPE);

            // Create a new project
            var project = trainingApi.CreateProject(Project_Name, null, objDetectionDomain.Id);
            Console.WriteLine($"\tProject '{ Project_Name }' was created.");

            // Make two tags in the new project
            var circleTag = trainingApi.CreateTag(project.Id, Tags.Cicle.GetDescription());
            var xTag = trainingApi.CreateTag(project.Id, Tags.X.GetDescription());

            Guid circleTagId = circleTag.Id;
            Guid xTagId = xTag.Id;

            ConcurrentDictionary<string, List<Region>> imgNamesToLabelRegions = new ConcurrentDictionary<string, List<Region>>();

            AddLabeledRegionsFromXML(imgNamesToLabelRegions, circleTagId, xTagId);

            AddLabeledRegionsFromImgGridFiles(imgNamesToLabelRegions, circleTagId, xTagId);

            var imageFileEntries = ReturnImageFileEntries(imgNamesToLabelRegions);

            for (int i = 0; i < Math.Ceiling((decimal)imageFileEntries.Count / BATCH_SIZE); i++)
            {
                var imageFileEntriesBatch = imageFileEntries.Skip(i * BATCH_SIZE).Take(BATCH_SIZE).ToList();

                trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFileEntriesBatch));

                Console.WriteLine($"\t{ imageFileEntriesBatch.Count } tagged images have been pushed to the project.");
            }

            // Now there are images with tags start training the project
            Console.WriteLine("\tStart of training process.");
            var iteration = trainingApi.TrainProject(project.Id);

            // The returned iteration will be in progress, and can be queried periodically to see when it has completed
            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                // Re-query the iteration to get its updated status
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            }
            Console.WriteLine("\tEnd of training process.");

            // The iteration is now trained. Publish it to the prediction end point.
            var predictionResourceId = CUSTOM_VISION_RESOURCE_ID;

            trainingApi.PublishIteration(project.Id, iteration.Id, Published_Model_Name, predictionResourceId);
            Console.WriteLine("Trained Project has been published!\n");
        }

        private static List<ImageFileCreateEntry> ReturnImageFileEntries(ConcurrentDictionary<string, List<Region>> imgNamesToLabelRegions)
        {
            var imageFileEntries = new ConcurrentBag<ImageFileCreateEntry>();

            Parallel.ForEach(Directory.GetFiles(@"../../../Resources/Training_Pictures"), (path) =>
            {
                string fileName = Path.GetFileName(path).Replace(IMG_FILE_EXTENSION, string.Empty);

                imageFileEntries.Add(new ImageFileCreateEntry(path, File.ReadAllBytes(path), null,
                    imgNamesToLabelRegions.ContainsKey(fileName)
                        ? imgNamesToLabelRegions[fileName]
                        : new List<Region>()));
            });

            return imageFileEntries.ToList();
        }

        private static void AddLabeledRegionsFromImgGridFiles(ConcurrentDictionary<string, List<Region>> imgNamesToLabelRegions, Guid circleTagId, Guid xTagId)
        {
            Parallel.ForEach(Directory.GetFiles(@"../../../Resources/Training_Pictures"), (path) => {

                if (path.ToLower().Contains(IMG_GRID_FILE_NAME_MARK))
                {
                    var repeatingImagesInfoParts = path.Substring(path.LastIndexOf('_') + 1).Replace(IMG_FILE_EXTENSION, string.Empty).Split(IMG_GRID_FILE_NAME_PARAM_SPLITTER).Select(x => int.Parse(x)).ToArray();

                    int repeatingElementRows = repeatingImagesInfoParts[0];
                    int repeatingElementColumns = repeatingImagesInfoParts[1];

                    int elementWidth = repeatingImagesInfoParts[2];
                    int elementHeight = repeatingImagesInfoParts[3];

                    string fileName = Path.GetFileName(path).Replace(IMG_FILE_EXTENSION, string.Empty);

                    imgNamesToLabelRegions[fileName] = new List<Region>();

                    for (int i = 0; i < repeatingElementRows; i++)
                    {
                        for (int j = 0; j < repeatingElementColumns; j++)
                        {
                            var top = (double)i / repeatingElementRows;
                            var left = (double)j / repeatingElementColumns;

                            var heigth = (double)1 / repeatingElementRows;
                            var width = (double)1 / repeatingElementColumns;

                            imgNamesToLabelRegions[fileName].Add(new Region()
                            {
                                TagId = circleTagId,
                                Left = left,
                                Top = top,
                                Width = width,
                                Height = heigth
                            });
                        }
                    }
                }
            });
        }

        private static void AddLabeledRegionsFromXML(ConcurrentDictionary<string, List<Region>> imgNamesToLabelRegions, Guid circleTagId, Guid xTagId)
        {
            Parallel.ForEach(Directory.GetFiles(@"../../../Resources/Training_Labels"), (path) => {

                var labelsDocRoot = XDocument.Load(path).Root;

                var fileName = labelsDocRoot.Element("filename").Value.Replace(IMG_FILE_EXTENSION, string.Empty);
                var imageWidth = int.Parse(labelsDocRoot.Element("size").Element("width").Value);
                var imageHeight = int.Parse(labelsDocRoot.Element("size").Element("height").Value);

                imgNamesToLabelRegions[fileName] = new List<Region>();

                foreach (var lblXML in labelsDocRoot.Elements("object"))
                {
                    string tagName = int.Parse(lblXML.Element("name").Value) == 0 ? Tags.Cicle.GetDescription() : Tags.X.GetDescription();

                    var labelBoundaryBox = lblXML.Element("bndbox");
                    var minMaxCoordinatesX = ReturnMinAndMaxCoordinate("x", labelBoundaryBox);
                    var minMaxCoordinatesY = ReturnMinAndMaxCoordinate("y", labelBoundaryBox);

                    var top = minMaxCoordinatesY.Item1 / imageHeight;
                    var left = minMaxCoordinatesX.Item1 / imageWidth;

                    var heigth = (minMaxCoordinatesY.Item2 - minMaxCoordinatesY.Item1) / imageHeight;
                    var width = (minMaxCoordinatesX.Item2 - minMaxCoordinatesX.Item1) / imageWidth;

                    imgNamesToLabelRegions[fileName].Add(new Region()
                    {
                        TagId = tagName == Tags.Cicle.GetDescription() ? circleTagId : xTagId,
                        Left = (double)left,
                        Top = (double)top,
                        Width = (double)width,
                        Height = (double)heigth
                    });
                }
            });
        }

        private static Tuple<decimal, decimal> ReturnMinAndMaxCoordinate(string coordeinate, XElement boundaryBox)
        {
            var list = new List<decimal>() { decimal.Parse(boundaryBox.Element(coordeinate + "min").Value), decimal.Parse(boundaryBox.Element(coordeinate + "max").Value) };

            return new Tuple<decimal, decimal>(list.Min(), list.Max());
        }
    }
}
