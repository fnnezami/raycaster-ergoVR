using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public class Raycaster : MonoBehaviour
{
    [SerializeField] private bool isRayCasting = false;
    [SerializeField] [Range(0, 10)] private float gizmosRayCastLength = 1;
    [SerializeField] private char separator= ',';
    [SerializeField] [Range(0, 10)] private float gizmosHelperRadius = 0.2f;

    
    
    public CancellationTokenSource cts;
    private CancellationToken masterToken;
    private CancellationToken readerToken;
    private CancellationToken casterToken;
    private CancellationToken writerToken;
    
    private Queue<string> unparsedBuffer;
    private Queue<List<string>> finalBuffer;
    private bool readFinished = false;
    
    
    private string csvHeaders = "";
    public string _fileName;
    public string FileName
    {
        get => _fileName;
        set => _fileName = value;
    }


    private Vector3 _gizmosEyePos;
    private Vector3 _gizmosEyeDir;
    private Vector3 _gizmosNosePos;
    private Vector3 _gizmosNoseDir;
    void Start()
    {
        cts = new CancellationTokenSource();
        masterToken = new CancellationToken();
        masterToken = cts.Token;
        
        _gizmosEyePos = new Vector3();
        _gizmosEyeDir = Vector3.forward;
        _gizmosNosePos = new Vector3();
        _gizmosNoseDir = Vector3.forward;
    }

    // Update is called once per frame
    private void OnDestroy()
    {
        StopRayCaster();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartRayCaster();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            StopRayCaster();
        }
    }

    private void StopRayCaster()
    {
        isRayCasting = false;
        cts.Cancel();
        cts = new CancellationTokenSource();
        masterToken = new CancellationToken();
        masterToken = cts.Token;
    }

    private async void StartRayCaster()
    {
        try
        {
            readerToken = new CancellationToken();
            casterToken = new CancellationToken();
            writerToken = new CancellationToken();
            
            readerToken = cts.Token;
            casterToken = cts.Token;
            writerToken = cts.Token;
            await Task.Run(() =>
            {
                isRayCasting = true;
                ReadCSV(_fileName, readerToken);
                ParseCSV(casterToken);
                WriteCSV(_fileName + "~", writerToken);
                masterToken.ThrowIfCancellationRequested();
            },masterToken);
        }
        catch (OperationCanceledException e)
        {
            Debug.LogWarning("All aborted!");
            throw;
        }
       
        
    }
    public async Task ReadCSV(string fileName, CancellationToken readToken)
    {
        readFinished = false;
        unparsedBuffer = new Queue<string>();
        bool isFirstLine = true;
        using (StreamReader csv = new StreamReader(fileName))
        {
            var csvLine = "";
            

            try
            {
                while ((csvLine = await Task.Run(() =>
                {
                    readToken.ThrowIfCancellationRequested();

                    return csvLine = csv.ReadLine();
                }, readToken)) != null)
                {
                    if (isFirstLine)
                    {
                        csvHeaders = csvLine;
                        isFirstLine = false;
                    }
                    else
                        unparsedBuffer.Enqueue(csvLine);
                    Debug.LogFormat("unparsed buffer size = {0}",unparsedBuffer.Count);
                }
                
                readFinished = true;
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning("Reader Task aborted");
                unparsedBuffer.Clear();
                throw;
            }
        }
    }
    public async Task ParseCSV(CancellationToken castToken)
    {
        finalBuffer = new Queue<List<string>>();
        try
        {
            await Task.Run(() =>
            {

                while (true)
                {
                    if (unparsedBuffer.Count != 0)
                    {
                        List<string> parsedLine = unparsedBuffer.Dequeue().Split(separator).ToList();
                        //Tuple<string, string> hitResult = CastRay(parsedLine);
                        ////////////////////////////////////////
                        string eyeHit = "";
                        string noseHit = "";
                        Vector3 eyeOrigin = new Vector3( float.Parse(parsedLine[7]),float.Parse(parsedLine[8]),float.Parse(parsedLine[9]));
                        Vector3 eyeDirection = new Vector3( float.Parse(parsedLine[10]),float.Parse(parsedLine[11]),float.Parse(parsedLine[12]));
                        Vector3 noseOrigin = new Vector3( float.Parse(parsedLine[13]),float.Parse(parsedLine[14]),float.Parse(parsedLine[15]));
                        Vector3 noseDirection = new Vector3( float.Parse(parsedLine[16]),float.Parse(parsedLine[17]),float.Parse(parsedLine[18]));
                        _gizmosEyePos = eyeOrigin;
                        _gizmosEyeDir = eyeDirection;
                        _gizmosNosePos = noseOrigin;
                        _gizmosNoseDir = noseDirection;
                        RaycastHit eyeRayHit = new RaycastHit();
                        RaycastHit noseRayHit = new RaycastHit();
                        Debug.LogWarningFormat("raycasting with eye origin ({0}), eye direction ({1}), nose origin ({2}) and nose direction ({3})",eyeOrigin,eyeDirection,noseOrigin,noseDirection);
                        if (Physics.Raycast(eyeOrigin, eyeDirection, out eyeRayHit, Mathf.Infinity))
                        {
                            eyeHit = eyeRayHit.collider.name;
                        }
                        if (Physics.Raycast(noseOrigin, noseDirection, out noseRayHit, Mathf.Infinity))
                        {
                            noseHit = noseRayHit.collider.name;
                        }
                        parsedLine[5] = eyeHit;
                        parsedLine[6] = noseHit;
                        //////////////////////////////////////// 
                        
//                        parsedLine[5] = hitResult.Item1;
//                        parsedLine[6] = hitResult.Item2;
                        finalBuffer.Enqueue(parsedLine);
                        Debug.LogFormat("final buffer size = {0}",finalBuffer.Count);
                    }
                    else
                    {
                        Debug.LogError("something is wrong");
                    }
                    castToken.ThrowIfCancellationRequested();
                    
                }
            }, castToken);
        }
        catch (OperationCanceledException e)
        {
            Debug.LogWarning("Cater Task aborted");
            finalBuffer.Clear();
            throw;
        }
        
    }
    public async Task WriteCSV(string fileName, CancellationToken writeToken)
    {
        
        using (StreamWriter csv = new StreamWriter(fileName))
        {
            try
            {
                while (csvHeaders == "")
                {
                    
                }
                csv.WriteLineAsync(csvHeaders);
                await Task.Run(() =>
                {

                    while (true)
                    {
                        if (finalBuffer.Count != 0)
                        {
                            string lineToWrite = "";
                            foreach (string section in finalBuffer.Dequeue())
                            {
                                lineToWrite += (section + separator);
                            }

                            csv.WriteLineAsync(lineToWrite);
                        }
                        if (readFinished && unparsedBuffer.Count == 0 && finalBuffer.Count == 0)
                        {
                            cts.Cancel();
                        }
                        writeToken.ThrowIfCancellationRequested();
                    }
                }, writeToken);
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning("Writer Task aborted");
                throw;
            }
        }
    }
    private Tuple<string, string> CastRay(List<string> line)
    {
        string eyeHit = "";
        string noseHit = "";
        Vector3 eyeOrigin = new Vector3( float.Parse(line[7]),float.Parse(line[8]),float.Parse(line[9]));
        Vector3 eyeDirection = new Vector3( float.Parse(line[10]),float.Parse(line[11]),float.Parse(line[12]));
        Vector3 noseOrigin = new Vector3( float.Parse(line[13]),float.Parse(line[14]),float.Parse(line[15]));
        Vector3 noseDirection = new Vector3( float.Parse(line[16]),float.Parse(line[17]),float.Parse(line[18]));
        _gizmosEyePos = eyeOrigin;
        _gizmosEyeDir = eyeDirection;
        _gizmosNosePos = noseOrigin;
        _gizmosNoseDir = noseDirection;
        RaycastHit eyeRayHit = new RaycastHit();
        RaycastHit noseRayHit = new RaycastHit();
        Debug.LogWarningFormat("raycasting with eye origin ({0}), eye direction ({1}), nose origin ({2}) and nose direction ({3})",eyeOrigin,eyeDirection,noseOrigin,noseDirection);
        if (Physics.Raycast(eyeOrigin, eyeDirection, out eyeRayHit, Mathf.Infinity))
        {
            eyeHit = eyeRayHit.collider.name;
        }
        if (Physics.Raycast(noseOrigin, noseDirection, out noseRayHit, Mathf.Infinity))
        {
            noseHit = noseRayHit.collider.name;
        }
        return new Tuple<string, string>(eyeHit,noseHit);
    }
    
    private void OnDrawGizmos()
    {
        if (isRayCasting)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_gizmosNosePos,gizmosHelperRadius);
            Gizmos.DrawRay(_gizmosNosePos, _gizmosNoseDir * gizmosRayCastLength);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_gizmosEyePos,gizmosHelperRadius);
            Gizmos.DrawRay(_gizmosEyePos, _gizmosEyeDir * gizmosRayCastLength);
        }
    }
}

[CustomEditor(typeof(Raycaster))]
public class RaycasterInspector : Editor
{
    private CancellationToken token;
    private CancellationToken casterToken;
    private CancellationToken writerToken;
    public override void OnInspectorGUI()
    {
        Raycaster raycaster = (Raycaster) target;
        base.OnInspectorGUI();
        if (GUILayout.Button("select csv file"))
            raycaster.FileName = EditorUtility.OpenFilePanel("CSV data file", Application.dataPath, "csv");
        
    }
}