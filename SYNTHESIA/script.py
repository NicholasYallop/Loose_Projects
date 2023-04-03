#pip install ffmpeg-python
import requests
import csv
#import ffmpeg

#SCRIPT, string without quotation marks // put headers in {} to call from csv columns
bulkscript = '{name} this {test} is a {id} test'

#header for key ID
authorizationheader = {'Authorization': '451eea0d0b932347a6fd439408013d39'}

#header for key ID and format request
createheaders = {
    'Authorization': '451eea0d0b932347a6fd439408013d39',
    'Content-Type': 'application/json',
}

#testdata = '{ "title": "Hello, World!", "description": "This is my first synthetic video, made with the Synthesia API!", "visibility": "public", "test": true, "callbackId": "john@example.com", "input": [{ "script": "This is my first synthetic video, made with the Synthesia API!", "actor": "santa_costume2_cameraA", "background": "green_screen", "soundtrack": "inspirational", "actorSettings": { "horizontalAlign": "center", "scale": 0.9 } } ] }'
#requests.post('https://api.synthesia.io/v1/videos', headers=createheaders, data=testdata)
#file locations
csv_location = 'synthesia.csv'
defaultdownload = 'synthesia.mp4'
bulkdownloaddirectory = 'F:/Code_Samples/SYNTHESIA/downloads'

#Default values for video creation
title = '"Python scripting test"'
description = '"Nick Yallop Python request automation test"'
visibility = '"public"'
test = 'true'
callbackId = '"nickyallop@googlemail.com"'
actor = '"anna_costume1_cameraA"'
#try actor = "santa_costume1_cameraA"
background = '"green_screen"'
soundtrack = '"inspirational"'
actoralign = '"center"'
actorscale = '0.9'
#function to split template string into script part and requested csv columns
def checkformarkers(scripttemplate):
    script = ''
    scripts = list()
    marker = ''
    markers = list()
    readid = 0
    script_no = 0
    for i in range(0,len(scripttemplate)):
        if scripttemplate[i] == '{':
            readid = 1
            scripts.append(script)
            script_no += 1
            script = ''
        elif scripttemplate[i] == '}':
            readid = 0
            markers.append(marker)
            marker = ''
        else:
            if readid == 0 :
                script += scripttemplate[i]
            elif readid == 1 :
                marker += scripttemplate[i]
    if script != scripts[script_no-1]:
        scripts.append(script)
    return(scripts, markers)
#get data from csv // format: list of columns
def getcsvdata(filename):
    columnlist = list()
    with open(filename) as csv_file:
        csv_reader = csv.reader(csv_file, delimiter =',')
        row_count=0
        for row in csv_reader:
            cell_count = 0
            for cell in row:
                if row_count == 0:
                    columnlist.append(list())
                columnlist[cell_count].append(cell)
                cell_count += 1
            row_count += 1
    return(columnlist)
#requests for video creation N.B. script requests must be strings including quote marks
def createvideo(titleinput,descriptioninput,visibilityinput,testinput,callbackIdinput,actorinput,backgroundinput,soundtrackinput,actoraligninput,actorscaleinput,scriptinput):
    createdata = '{ "title": '+titleinput+', "description": '+descriptioninput+', "visibility": '+visibilityinput+', "test": '+testinput+', "callbackId": '+callbackIdinput+', "input": [{ "script": '+scriptinput+', "actor": '+actorinput+', "background": '+backgroundinput+', "soundtrack": '+soundtrackinput+', "actorSettings": { "horizontalAlign": '+actoraligninput+', "scale": '+actorscaleinput+' } } ] }'
    return(requests.post('https://api.synthesia.io/v1/videos', headers=createheaders, data=createdata))
def createvideoauto(scriptrequest):
    return(createvideo(title,description,visibility,test,callbackId,actor,background,soundtrack,actoralign,actorscale,scriptrequest))
#request for videos from csv list of names N.B. script templates can be just string of text 
def createbulkvideo(filename, scripttemplate):
    scriptlist = list()
    scriptchecked = checkformarkers(scripttemplate)
    scripts = scriptchecked[0]
    markers = scriptchecked[1]
    data = getcsvdata(filename)
    for i in data: #check for duplicate columns with incompatible data
        for j in data:
            if i[0] == j[0] and i != j:
                print("ERROR: repeat columns in csv")
                break
    markertocolumn = list() #list for marker to header transform
    marker_count = 0
    for marker in markers:
        markertocolumn.append('')
        column_count = 0
        for column in data:
            if column[0] == marker:
                markertocolumn[marker_count] = column_count
            column_count += 1
        marker_count += 1        
    marker_count = 0 
    script_count = 0
    for i in range(1,len(data[0])):#create list of scripts
        scriptlist.append('"')
    for marker in markers:
        cell_count = 0
        for cell in data[markertocolumn[marker_count]][1:]:#populate by script part, then cell value 
            scriptlist[cell_count] += scripts[script_count]
            scriptlist[cell_count] += cell
            if marker_count == len(markers)-1: #adds final piece of script if present
                if marker_count != len(scripts)-1:
                    scriptlist[cell_count] += scripts[marker_count+1]
            cell_count += 1
        marker_count += 1
        script_count += 1
    for i in range(0,len(data[0])-1):#closes list of scripts
        scriptlist[i] += '"'     
    id_count = 0
    bulktitle = str()
    for k in scriptlist:
        bulktitle = '"' + data[0][id_count+1] + '"'
        id_count += 1
        createvideo(bulktitle,description,visibility,test,callbackId,actor,background,soundtrack,actoralign,actorscale,k)
    return(id_count)
#request for list of video 
def listvideos():
    return(requests.get('https://api.synthesia.io/v1/videos', headers=authorizationheader))
def returnId(i):
    return(listvideos().json()["videos"][i]['id'])
#request for video details 
def videodetails(videoIdinput):
    videodomain = 'https://api.synthesia.io/v1/videos/' + videoIdinput
    return(requests.get(videodomain, headers=authorizationheader))
#requests to download video
def downloadvideo(videoIdinput, downloadlocation):
    while videodetails(videoIdinput).json()['status'] != 'COMPLETE':
        print("processing")
    downloadlink = videodetails(videoIdinput).json()['download']
    open(downloadlocation, "wb").write(requests.get(downloadlink).content)
    return(requests.get(downloadlink))
def downloadrecentvideo(i, downloadlocation):
    return (downloadvideo(returnId(i), downloadlocation))
def downloadbulkvideo(downloaddirectory, i):
    for j in range(0,i):
        videoId = returnId(j)
        currentvideodetails = videodetails(videoId).json()
        downloadlocation = downloaddirectory + '/' + currentvideodetails['title'] + '.mp4'
        downloadvideo(videoId, downloadlocation)
#turn green screen into image
#def greenscreenreplace(vidnumber):
#    csvid = str(vidnumber)
#    image = ffmpeg.input('F:/SYNTHESIA/background.jpg')
#    videofile = ffmpeg.input('downloads/'+csvid+'.mp4')
#    videofile = ffmpeg.colorchannelmixer(videofile, green=0)
#    output = ffmpeg.overlay(image, videofile)
#    out1 = ffmpeg.output(output, 'F:/SYNTHESIA/output.mp4', f='mp4')
#    out2 = output.output('F:/SYNTHESIA/output.mp4', vcodec="copy", acodec="copy", f='mp4')
#    ffmpeg.output(ffmpeg.input(out2), 'F:/SYNTHESIA/output.webm')
#    print(ffmpeg.get_args(out2))
#    #open('output.mp4',"w").write(out)

#OUTPUT 
#createvideoauto('"test"')
videocount = createbulkvideo(csv_location, bulkscript)
downloadbulkvideo(bulkdownloaddirectory, videocount)


    