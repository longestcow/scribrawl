using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class Draw : MonoBehaviour
{   
    public Camera m_camera;
    public GameObject brush;

    public string templateName = "rect";
    public bool save = true;
    public int lineCap = 200;

    LineRenderer currentLineRenderer;
    GameObject previousDrawing;

    Vector2 lastPos;

    DollarRecognizer recog;
    GameManager manager;

    public player player;
    void Start()
    {

        
        manager = Camera.main.gameObject.GetComponent<GameManager>();
        recog = manager.dollar;
    }

    void Update()
    {
        if(!player.grabbed && !player.hovering)
            Drawing();
    }

    void Drawing() 
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)){
            if(previousDrawing!=null) Destroy(previousDrawing);
            CreateBrush();
        }
        else if (Input.GetKey(KeyCode.Mouse0)){
            PointToMousePos();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0)){
            Vector3[] positions3 = new Vector3[currentLineRenderer.positionCount];
            currentLineRenderer.GetPositions(positions3);
            Vector2[] positions2 = System.Array.ConvertAll<Vector3, Vector2> (positions3, getV2fromV3);
            if(save) {
                string text = templateName+":";
                for (int i = 0; i<positions2.Length; i++){
                    text+=positions2[i].x+","+positions2[i].y+";";
                }
                text=text.Substring(0,text.Length-1);
                GUIUtility.systemCopyBuffer = text;
                print("copied to clipboard new "+templateName); //copy to clipboard
                manager.storeData(text, templateName);
                recog.SavePattern(templateName, positions2); // save for current instance as well
            }

            DollarRecognizer.Result result = recog.Recognize(positions2);
            if(result.Match==null)
                return;
            print(result.ToString());

            if(result.Match.Name=="rect"){
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                foreach(Vector2 point in positions2){
                    if (point.x < minX) minX = point.x;
                    if (point.x > maxX) maxX = point.x;
                    if (point.y < minY) minY = point.y;
                    if (point.y > maxY) maxY = point.y;
                }
                PolygonCollider2D polyCol = previousDrawing.AddComponent<PolygonCollider2D>();
                polyCol.points = new Vector2[] { // create a box collider basically
                    new Vector2(minX, minY),
                    new Vector2(maxX, minY),
                    new Vector2(maxX, maxY),
                    new Vector2(minX, maxY),
                };
                previousDrawing.AddComponent<Rigidbody2D>().gravityScale = 0;
                currentLineRenderer.enabled = false;
                manager.walls.Add(polyCol);
                previousDrawing = null;
            }

            currentLineRenderer = null;
        }
        else  {
            currentLineRenderer = null;
        }


    }

    void CreateBrush() 
    {
        GameObject brushInstance = Instantiate(brush);
        currentLineRenderer = brushInstance.GetComponent<LineRenderer>();
        previousDrawing = brushInstance;
        Vector2 mousePos = m_camera.ScreenToWorldPoint(Input.mousePosition);

        currentLineRenderer.SetPosition(0, mousePos);
        currentLineRenderer.SetPosition(1, mousePos);

    }

    void AddAPoint(Vector2 pointPos) 
    {
        currentLineRenderer.positionCount++;
        int positionIndex = currentLineRenderer.positionCount - 1;
        currentLineRenderer.SetPosition(positionIndex, pointPos);

        float totalLength = 0;
        Vector3[] positions = new Vector3[currentLineRenderer.positionCount];
        currentLineRenderer.GetPositions(positions);
        for (var i = 1; i < currentLineRenderer.positionCount; i++) {
            totalLength += Vector3.Distance(positions[i - 1], positions[i]);
        }


        if(totalLength > lineCap) {
            float newLength = 0;
            List<Vector3> points = new List<Vector3>();
            for(int i = currentLineRenderer.positionCount-1; i>0; i--){
                points.Add(currentLineRenderer.GetPosition(i));
                newLength += Vector3.Distance(currentLineRenderer.GetPosition(i), currentLineRenderer.GetPosition(i-1));
                if(newLength > lineCap) 
                    break;
            }
            if(newLength <= lineCap)
                points.Add(currentLineRenderer.GetPosition(0));
            points.Reverse();
            currentLineRenderer.positionCount = points.Count;
            currentLineRenderer.SetPositions(points.ToArray());
        }


    }

    void PointToMousePos() 
    {
        Vector2 mousePos = m_camera.ScreenToWorldPoint(Input.mousePosition);
        if (lastPos != mousePos) 
        {
            AddAPoint(mousePos);
            lastPos = mousePos;
        }
    }


		
	public static Vector2 getV2fromV3 (Vector3 v3)
	{
		return new Vector2 (v3.x, v3.y);
	}

}