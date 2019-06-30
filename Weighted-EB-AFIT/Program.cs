using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ContainerPacking;
using Weighted_EB_AFIT.Properties;

namespace Weighted_EB_AFIT
{
    class Program
    {
        static void Main()
        {
            using (var memoryStream = new MemoryStream(Resources.ORLibrary))
            using (var reader = new StreamReader(memoryStream))
            {
                // Counter to control how many tests are run in dev.
                var stopwatch = Stopwatch.StartNew();
                int counter = 1;

                while (reader.ReadLine() != null && counter <= 700)
                {
                    List<Item> itemsToPack = new List<Item>();

                    // First line in each test case is an ID. Skip it.

                    // Second line states the results of the test, as reported in the EB-AFIT master's thesis, appendix E.
                    string[] testResults = reader.ReadLine().Split(' ');

                    // Third line defines the container dimensions.
                    string[] containerDims = reader.ReadLine().Split(' ');

                    // Fourth line states how many distinct item types we are packing.
                    int itemTypeCount = Convert.ToInt32(reader.ReadLine());

                    for (int i = 0; i < itemTypeCount; i++)
                    {
                        string[] itemArray = reader.ReadLine().Split(' ');
                        var size = new Vector3(Convert.ToDouble(itemArray[1]), Convert.ToDouble(itemArray[3]), Convert.ToDouble(itemArray[5]));
                        Item item = new Item(string.Intern(size.ToString()), size, Convert.ToInt32(itemArray[7]), 0);
                        itemsToPack.Add(item);
                    }

                    List<Vector3> containers = new List<Vector3>();
                    containers.Add(new Vector3(Convert.ToDouble(containerDims[0]), Convert.ToDouble(containerDims[1]), Convert.ToDouble(containerDims[2])));
                    var result = containers.Select((x) => { Console.WriteLine($"{counter}/{700}"); return EB_AFIT.Run(x, itemsToPack.ToArray()); }).ToArray();

                    // Assert that the number of items we tried to pack equals the number stated in the published reference.
                    if (result[0].PackedItems.Length + result[0].UnpackedItems.Length != Convert.ToInt32(testResults[1]))
                    {
                        Console.WriteLine($"Error1: Goal = {Convert.ToInt32(testResults[1])}, Result = {result[0].PackedItems.Length + result[0].UnpackedItems.Length}");
                        break;
                    }

                    // Assert that the number of items successfully packed equals the number stated in the published reference.
                    if (result[0].PackedItems.Length != Convert.ToInt32(testResults[2]))
                    {
                        Console.WriteLine($"Error2: Goal = {Convert.ToInt32(testResults[2])}, Result = {result[0].PackedItems.Length}");
                    }

                    counter++;
                }

                Console.WriteLine("DONE " + stopwatch.Elapsed);
                Console.ReadLine();
            }
        }
    }
}
