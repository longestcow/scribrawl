using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public DollarRecognizer dollar = new DollarRecognizer();
    [HideInInspector] public string data = "Assets/Resources/trainingData.txt";
    [HideInInspector] public List<PolygonCollider2D> walls = new List<PolygonCollider2D>(), arrows = new List<PolygonCollider2D>();
    [Header("wall")] 
    public float wallMass;
    public float wallDrag;
    [Header("arrow")] 
    public float arrowMass;
    public float arrowDrag;
    public float arrowVelocity;
    void Start() // load txt file into dollar
    {
        if(!File.Exists(data)){
            print("training data not found");
            return;
        }

        using(StreamReader reader = new StreamReader(data)){
            string line;
            while((line = reader.ReadLine()) != null){
                string[] parts = line.Split(":"), xy;
                List<Vector2> points = new List<Vector2>();
                string name = parts[0];
                string[] vectors = parts[1].Split(";");
                for(int i = 0; i<vectors.Length; i++){
                    xy = vectors[i].Split(",");
                    points.Add(new Vector2(float.Parse(xy[0]), float.Parse(xy[1])));
                }
                print("loaded "+name);
                dollar.SavePattern(name, points);

            }
            print("finished loading");
        }
    }

    public void storeData(string text, string name){
        using(StreamWriter writer = new StreamWriter(data, true)){
            writer.WriteLine(text);
        }
        print("written to file new "+name);
    }

    void Update()
    {
        foreach(PolygonCollider2D col in walls){
            if(col == null) continue;
            Vector2[] points = col.points; 

            for (int i = 0; i < points.Length; i++) {
                
                Vector2 a = col.transform.TransformPoint(points[i]);
                Vector2 b = col.transform.TransformPoint(points[(i + 1) % points.Length]); 

                Debug.DrawLine(a, b, Color.white);
            }
        }
        foreach(PolygonCollider2D col in arrows){
            if(col == null) continue;

            Vector2[] points = col.points; 

            for (int i = 0; i < points.Length; i++) {
                
                Vector2 a = col.transform.TransformPoint(points[i]);
                Vector2 b = col.transform.TransformPoint(points[(i + 1) % points.Length]); 

                Debug.DrawLine(a, b, Color.white);
            }
        }
        
    }

}
