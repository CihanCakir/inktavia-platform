using MongoDB.Driver.GeoJsonObjectModel;

namespace Aizen.Core.Data.Mongo.Extensions
{
    public static class MongoExtensions
    {
        public static GeoJson2DGeographicCoordinates GetCoordinates(double longitude, double latitude)
        {
            return new GeoJson2DGeographicCoordinates(longitude, latitude);
        }

        public static GeoJsonPoint<GeoJson2DGeographicCoordinates> GetJsonPoint(double x, double y)
        {
            GeoJson2DGeographicCoordinates output = GetCoordinates(x, y);
            if (output == null)
            {
                return null;
            }

            return new GeoJsonPoint<GeoJson2DGeographicCoordinates>(output);
        }
        public static bool CheckValueIsValid(decimal? x, decimal? y)
        {
            if (x.HasValue == false || y.HasValue == false)
            {
                return false;
            }
            else
            {
                if (x == y)
                {
                    return false;
                }
                else if (((x < -180 || x > 180) && (y < -90 || y > 90)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
