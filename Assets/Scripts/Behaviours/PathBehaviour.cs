using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurveDemo
{
    [ExecuteAlways]
    public partial class PathBehaviour : MonoBehaviour
    {
        [SerializeField] Path path;
        public Path.FrameCache Frames => path.Cache;

        void Awake()
        {            
            path.InitCache();           
        }        
    }
}