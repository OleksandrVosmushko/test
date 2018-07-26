using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Common.Geometry;
using IntervalTree;

namespace s2geometrytest
{
    class AnotherIndex
    {
        private int _level;
        private IntervalTree<UserList> rtree;
        private SortedDictionary<Guid, S2CellId> _currentUsersLocations;

        public struct UserList : IComparable<UserList>
        {
            public S2CellId s2CellId;

            public List<Guid> list;
            
            public int CompareTo(UserList other)
            {
                return s2CellId.CompareTo(other.s2CellId);
            }
        }

        public AnotherIndex(int level)
        {
            rtree = new IntervalTree<UserList>();
            _level = level;
            _currentUsersLocations = new SortedDictionary<Guid, S2CellId>();
        }

        public void AddUser(Guid uid, double lon, double lat)
        {
            var lonLat = S2LatLng.FromDegrees(lat, lon);

            var cellId = S2CellId.FromLatLng(lonLat);

            var cellIdStorageLevel = cellId.ParentForLevel(_level);

            //var userList = new UserList { s2CellId = cellIdStorageLevel, list = new List<Guid>() };
            _currentUsersLocations[uid] = cellIdStorageLevel;

            var query_res = rtree.Search(new UserList(){s2CellId = cellIdStorageLevel });

            var users = new List<Guid>();
            if (query_res.Count > 0)
            {
               
                foreach (var item in query_res)
                {
                    users.AddRange(item.Start.list);
                }
                
                
                rtree.Remove(query_res[0]);

            }
            users.Add(uid);

            var toinsert = new UserList() { s2CellId = cellIdStorageLevel, list = users };

            
            rtree.Add(new Interval<UserList>(){Start = toinsert, End = toinsert});

        }

        public List<Guid> Search(double lon, double lat, int radius)
        {
            var latlng = S2LatLng.FromDegrees(lat, lon);

            var centerPoint = Index.pointFromLatLng(lat, lon);

            var centerAngle = ((double)radius) / Index.EarthRadiusM;

            var cap = S2Cap.FromAxisAngle(centerPoint, S1Angle.FromRadians(centerAngle));

            var regionCoverer = new S2RegionCoverer();

            regionCoverer.MaxLevel = 13;

            //  regionCoverer.MinLevel = 13;


            //regionCoverer.MaxCells = 1000;
            // regionCoverer.LevelMod = 0;


            var covering = regionCoverer.GetCovering(cap);



            var res = new List<Guid>();


            foreach (var u in covering)
            {
                var sell = new S2CellId(u.Id);

                if (sell.Level < _level)
                {
                    var begin = sell.ChildBeginForLevel(_level);
                    var end = sell.ChildEndForLevel(_level);

                    var qres = rtree.Search(new Interval<UserList>(new UserList(){s2CellId = begin}, new UserList(){s2CellId = end}));


                    foreach (var item in qres)
                    {

                        res.AddRange(item.Start.list);
                    }
                }
                else
                {
                    var qres = rtree.Search(new UserList() { s2CellId = sell });
                    if (qres.Count > 0)
                    {
                        foreach (var r in qres)
                        {
                            res.AddRange(r.Start.list);
                        }
                    }
                }
            }
            return res;
        }


        public bool RemoveUser(Guid uid)
        {
            var cell = _currentUsersLocations[uid];

            
            var qres = rtree.Search(new Interval<UserList>(new UserList() { s2CellId = cell }, new UserList() { s2CellId = cell}));
            //var clone = query_res.ToList();

            foreach (var q in qres)
            {
                var toremove = q.Start.list.FirstOrDefault(s => s == uid);

                if (toremove == null)
                    return false;
                q.Start.list.Remove(toremove);

            }

            //if (query_res.Count > 0)
            //{
            //    rtree.Remove(query_res[0]);
            //    if (clone.Count != 0)
            //    {
            //        rtree.Add(new SimpleRangeItem { Range = new Range<S2CellId>(clone[0].Range.From), Content = clone[0].Content });
            //    }

            //}
            //else return false;
           
            return true;
        }
    }
}
