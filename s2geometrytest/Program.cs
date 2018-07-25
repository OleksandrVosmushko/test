using Google.Common.Geometry;
using System;

namespace s2geometrytest
{
    class Program
    {
        
        static void Main(string[] args)
        {


            //    var point = new S2Point();

            //var cell = new S2CellId();


            //var point = Index.pointFromLatLng(47.678956162876, -122.12871664749933);

            //var latlon = new S2LatLng(point);

            //var lat = S2LatLng.FromDegrees(47.678956162876, -122.12871664749933);

            //S2CellId id = S2CellId.FromLatLng(lat);

            //var lon = id.ToLatLng();


            var i = new Index(13);

            i.AddUser(Guid.NewGuid(), 14.1313, 14.1313);

            i.AddUser(Guid.NewGuid(), 14.1314, 14.1314);

            i.AddUser(Guid.NewGuid(), 14.1311, 14.1311);

            i.AddUser(Guid.NewGuid(), 14.2313, 14.2313);

            i.AddUser(Guid.NewGuid(), 14.0313, 14.0313);

            TestSearch(i);

            Console.WriteLine();
        }

        public static void TestSearch(Index i)
        {
            var found = i.Search(14.1313, 14.1313, 20000);

            return;
        }

     

    }
}
