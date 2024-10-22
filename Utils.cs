using MongoDB.Bson;

namespace teachers_lounge_server
{
    public static class Utils
    {
        public static bool IsObjectId(this string potentialId)
        {
            var empty = ObjectId.Empty;

            return ObjectId.TryParse(potentialId, out empty);
        }
        public static T[] Merge<T>(params T[][] arrays)
        {
            if (arrays.Length < 1) return new T[0];
            if (arrays.Length < 2) return arrays[0];
            if (arrays.Length == 2)
            {
                T[] res = new T[arrays[0].Length + arrays[1].Length];
                arrays[0].CopyTo(res, 0);
                arrays[1].CopyTo(res, arrays[0].Length);

                return res;
            }

            T[] finalRes = new T[0];

            foreach (T[] array in arrays)
            {
                finalRes = Merge(finalRes, array);
            }

            return finalRes;
        }
    }
}
