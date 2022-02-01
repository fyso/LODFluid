using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class Singleton<T> where T : new()
    {
        private static T uniqueInstance;

        protected Singleton(){}

        public static T GetInstance()
        {
            if (uniqueInstance == null)
            {
                uniqueInstance = new T();
            }
            return uniqueInstance;
        }
    }
}