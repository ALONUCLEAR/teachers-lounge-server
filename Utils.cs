using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Bson;
using System.Security.Cryptography;
using System.Text;

namespace teachers_lounge_server
{
    public static class Utils
    {
        public static bool IsObjectId(this string potentialId)
        {
            var empty = ObjectId.Empty;

            return ObjectId.TryParse(potentialId, out empty);
        }
        public static T? Find<T>(this T[] arr, Predicate<T> predicate) where T : class
        {
            foreach (T val in arr)
            {
                if (predicate(val))
                {
                    return val;
                }
            }

            return null;
        }

        public static T[] Filter<T>(this T[] arr, Predicate<T> predicate)
        {
            return arr.FilterAndMap(predicate, x => x);
        }

        public static TOut[] Map<TIn, TOut>(this TIn[] arr, Func<TIn, TOut> mapper)
        {
            TOut[] result = new TOut[arr.Length];

            for(int i = 0; i < arr.Length; i++)
            {
                result[i] = mapper(arr[i]);
            }

            return result;
        }

        public static TOut[] FilterAndMap<TIn, TOut>(this TIn[] arr, Predicate<TIn> predicate, Func<TIn, TOut> mapper)
        {
            List<TOut> result = new List<TOut>();

            foreach(TIn val in arr)
            {
                if (predicate(val))
                {
                    result.Add(mapper(val));
                }
            }

            return result.ToArray();
        }

        public static bool Some<T>(this T[] arr, Predicate<T> predicate)
        {
            foreach (T val in arr)
            {
                if (predicate(val))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Every<T>(this T[] arr, Predicate<T> predicate)
        {
            Predicate<T> oppositePredicate = (T val) => !predicate(val);

            return !arr.Some(oppositePredicate);
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

        public static T RandomElement<T>(this T[] arr)
        {
            Random rand = new Random();
            int index = rand.Next(arr.Length);

            return arr[index];
        }

        public static char[] defaultCharSet
        {
            get
            {
                int length = 26 * 2 + 10;
                char[] charset = new char[length];

                for (int i = 0; i < length; i++)
                {
                    if (i < 10)
                    {
                        charset[i] = $"{i}"[0];
                    } else if (i < 36)
                    {
                        charset[i] = (char)('a' + i - 10);
                    } else
                    {
                        charset[i] = (char)('A' + i - 36);
                    }
                }

                return charset;
            }
        }

        public static string GenerateCode(this char[] possibleChars, int codeLength)
        {
            string code = "";

            for (int i = 0; i < codeLength; i++)
            {
                code += possibleChars.RandomElement();
            }

            return code;
        }

        public static string GenerateCode(this string possibleChars, int codeLength)
        {
            return GenerateCode(possibleChars.ToCharArray(), codeLength);
        }

        public static string GenerateCode(int codeLength)
        {
            return GenerateCode(defaultCharSet, codeLength);
        }

        public static TValue? GetValueOrDefault<TValue>(this BsonDocument document, string fieldName)
        {
            if (document.TryGetValue(fieldName, out BsonValue value))
            {
                return value.IsBsonNull ? default : value.ToNullable<TValue>() ?? default;
            }

            return default;
        }

        private static TValue? ToNullable<TValue>(this BsonValue value)
        {
            if (value == null) return default;
            try
            {
                return (TValue)BsonTypeMapper.MapToDotNetValue(value);
            }
            catch
            {
                return default;
            }
        }
        /// <summary>
        /// For an object array it would still only copy the refrences inside and not the object structure, but the array reference would be different.
        /// Perfect for primitive array copying.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalArray"></param>
        /// <returns></returns>
        public static T[] ShallowClone<T>(this T[] originalArray)
        {
            T[] clone = new T[originalArray.Length];
            Array.Copy(originalArray, clone, originalArray.Length);

            return clone;
        }

        public static string Hash(this string password)
        {
            string hashedPassword = "";

            using (HashAlgorithm sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var hashedBytes = sha256.ComputeHash(passwordBytes);
                hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }

            return hashedPassword;
        }
        public static string Join<T>(this IEnumerable<T> lst, string delimeter) where T: notnull
        {
            string joinedString = lst.ElementAt(0).ToString() ?? "";

            for (int i = 1; i < lst.Count(); i++)
            {
                joinedString += delimeter + lst.ElementAt(i).ToString();
            }

            return joinedString;
        }

        public static string Join<T>(this T[] arr, string delimeter) where T: notnull
        {
            string joinedString = arr[0].ToString() ?? "";

            for (int i = 1; i < arr.Length; i++)
            {
                joinedString += delimeter + arr[i].ToString();
            }

            return joinedString;
        }
    }
}
