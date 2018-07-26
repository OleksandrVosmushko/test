using BTree;
using Google.Common.Geometry;
using RangeTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace s2geometrytest
{
    class UserList
    {
        public S2CellId s2CellId;

        public List<Guid> list; 
    }
    public class SimpleRangeItem : IRangeProvider<S2CellId>
    {
        public Range<S2CellId>  Range { get; set; }

        public List<Guid> Content { get; set; }
    }

    public class SimpleRangeItemComparer : IComparer<SimpleRangeItem>
    {
        public int Compare(SimpleRangeItem x, SimpleRangeItem y)
        {
            return x.Range.CompareTo(y.Range);
        }
    }
    class IndexWithRange
    {
        public RangeTree<S2CellId, SimpleRangeItem> rtree;

        private int _level;
        private SortedDictionary<Guid, S2CellId> _currentUsersLocations;

        public IndexWithRange(int level)
        {
            rtree = new RangeTree<S2CellId, SimpleRangeItem>(new SimpleRangeItemComparer());
            _level = level;
            _currentUsersLocations = new SortedDictionary<Guid, S2CellId>();
        }

        public void AddUser(Guid uid, double lon, double lat)
        {
            var lonLat = S2LatLng.FromDegrees(lat, lon);

            var cellId = S2CellId.FromLatLng(lonLat);

            var cellIdStorageLevel = cellId.ParentForLevel(_level);

            //var userList = new UserList { s2CellId = cellIdStorageLevel, list = new List<Guid>() };

            var query_res = rtree.Query(cellIdStorageLevel);
            _currentUsersLocations[uid] = cellIdStorageLevel;
            SimpleRangeItem rangeItem =null;

            if (query_res.Count > 0 )
            {
                var users = new List<Guid>();
                foreach (var item in query_res)
                {
                    users.AddRange(item.Content);
                }
                
                rangeItem = new SimpleRangeItem { Range = new Range<S2CellId>(cellIdStorageLevel), Content = users };
                
                rtree.Remove(query_res[0]);

            }
            
            if (rangeItem == null)
            {
                rangeItem = new SimpleRangeItem { Range = new Range<S2CellId>(cellIdStorageLevel), Content = new List<Guid> ()};
            }
            rangeItem.Content.Add(uid);

            rtree.Add(rangeItem);
        }
        public  bool RemoveUser(Guid uid)
        {
            var cell = _currentUsersLocations[uid];

            var query_res = rtree.Query(cell);

            //var clone = query_res.ToList();

            foreach (var q in query_res)
            {
                var toremove = q.Content.FirstOrDefault(u => u == uid);

                if (toremove == null)
                    return false;
                q.Content.Remove(toremove);

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
            rtree.Rebuild();
            return true;
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

                    var qres = rtree.Query(new Range<S2CellId>(begin, end));

                    foreach(var r in qres)
                    {
                        res.AddRange(r.Content);
                    }
                }
                else
                {
                    var qres = rtree.Query(new Range<S2CellId>(sell));
                    if (qres.Count >0)
                    {
                        foreach (var r in qres)
                        {
                            res.AddRange(r.Content);
                        }
                    }
                }
            }
            return res;
        }

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

        public const double EarthRadiusM = 6371010.0;

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

            var regionCoverer = new S2RegionCoverer() ;

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
                    do
                    {
                        var cur = tree.Search(new S2CellId(begin.Id));

                        if (cur != null)
                        {
                            res.AddRange(cur.Pointer);

                        }

                        begin = begin.Next;
                    } while (begin.Id != end.Id);
                    
                }
                else
                {
                    var item = tree.Search(sell);
                    if (item != null)
                    {
                        res.AddRange(item.Pointer);
                    }
                }
            }
            return res;
        }

    }
}
