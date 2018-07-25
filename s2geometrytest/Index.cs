using BTree;
using Google.Common.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace s2geometrytest
{
    class UserList
    {
        public S2CellId s2CellId;

        public List<Guid> list; 
    }

    class Index
    {
        public static S2Point pointFromLatLng(double lat, double lon)
        {
            var phi = ConvertToRadians(lat);
            var theta = ConvertToRadians(lon);
            var cosPhi = Math.Cos(phi);
            return new S2Point(Math.Cos(theta) * cosPhi, Math.Sin(theta) * cosPhi, Math.Sin(phi));
        }
        public static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        const double EarthRadiusM = 6371010.0;

        private int _level;

        BTree<S2CellId, List<Guid>> tree;

        public Index ( int level = 30 )
        {
            tree = new BTree<S2CellId, List<Guid>>(35);
            _level = level;
        }

        public void AddUser(Guid uid, double lon, double lat)
        {
            var lonLat = S2LatLng.FromDegrees(lat, lon);

            var cellId = S2CellId.FromLatLng(lonLat);

            var cellIdStorageLevel = cellId.ParentForLevel(_level);

            var userList = new UserList { s2CellId = cellIdStorageLevel, list = new List<Guid>() };

            var item = tree.Search(userList.s2CellId);

            if (item != null)
            {
                userList = new UserList { s2CellId = item.Key, list  = item.Pointer};

                tree.Delete(userList.s2CellId);
            }

            if (userList.list == null)
            {
                userList.list = new List<Guid>();
                
            }
            userList.list.Add(uid);

            tree.Insert(userList.s2CellId, userList.list);
        }

        public List<Guid> Search (double lon, double lat, int radius) 
        {
            var latlng = S2LatLng.FromDegrees(lat, lon);

            var centerPoint = pointFromLatLng(lat,lon);

            var centerAngle = ((double)radius) / EarthRadiusM;

            var cap = S2Cap.FromAxisAngle(centerPoint, S1Angle.FromRadians(centerAngle));

            var regionCoverer = new S2RegionCoverer() { MinLevel = 15, MaxLevel = 5, MaxCells = 1000};

            var covering = regionCoverer.GetCovering(cap);

            var res = new List<Guid>();

            foreach (var u in covering)
            {
                var sell = new S2CellId(u.Id);
                var item = tree.Search(sell);
                if (item != null)
                {
                    res.AddRange(item.Pointer);
                }
            }
            return res;
        }

    }
}
