var UpnpControlPoint = require("./lib/upnp-controlpoint").UpnpControlPoint;
var wemo = require("./lib/wemo");
var http = require("http");

var urlBase = "http://localhost/?";
var lastBinaryState = {};
var devices = [];
var switches = [];
var handleDevice = function (device) {
    switch (device.deviceType) {
        case wemo.WemoControllee.deviceType:
            console.log("{Found " + device.friendlyName + "}");
            //console.log("device name: " + device.friendlyName + " device type: " + device.deviceType + " location: " + device.location);
            lastBinaryState[device.uuid] = 0;
            var wemoControllee = new wemo.WemoControllee(device);
            wemoControllee.on("BinaryState", function (value) {
                var oldValue = lastBinaryState[this.device.uuid];
                lastBinaryState[this.device.uuid] = value;
                if (!notify || typeof oldValue === "undefined") return;
                if (oldValue != value || value == 1) {
                    clearTimeout(timeout);
                    timeout = setTimeout(restart, restartTime);
                    lastBinaryState[this.device.uuid] = value;
                    var url = urlBase + escape(this.device.deviceType + ":" + this.device.friendlyName + ":" + value);
                    //console.log(url);
                    var req = http.get(url, function (res) { });
                    req.on("error", function (e) { console.log("http.get error " + e.message); });
                    req.setTimeout(500, function () { req.abort(); });
                }
            });
            devices.push(wemoControllee);
            switches.push(wemoControllee);
            wemoControllee.getBinaryState();
            break;
        case wemo.WemoLightswitch.deviceType:
            console.log("{Found " + device.friendlyName + "}");
            //console.log("device name: " + device.friendlyName + " device type: " + device.deviceType + " location: " + device.location);
            lastBinaryState[device.uuid] = 0;
            var wemoLightswitch = new wemo.WemoLightswitch(device);
            wemoLightswitch.on("BinaryState", function (value) {
                var oldValue = lastBinaryState[this.device.uuid];
                lastBinaryState[this.device.uuid] = value;
                if (!notify || typeof oldValue === "undefined") return;
                if (oldValue != value || value == 1) {
                    clearTimeout(timeout);
                    timeout = setTimeout(restart, restartTime);
                    var url = urlBase + escape(this.device.deviceType + ":" + this.device.friendlyName + ":" + value);
                    //console.log(url);
                    var req = http.get(url, function (res) { });
                    req.on("error", function (e) { console.log("http.get error " + e.message); });
                    req.setTimeout(500, function () { req.abort(); });
                }
            });
            devices.push(wemoLightswitch);
            switches.push(wemoLightswitch);
            wemoLightswitch.getBinaryState();
            break;
        case wemo.WemoSensor.deviceType:
            console.log("{Found " + device.friendlyName + "}");
            //console.log("device name: " + device.friendlyName + " device type: " + device.deviceType + " location: " + device.location);
            lastBinaryState[device.uuid] = 0;
            var wemoSensor = new wemo.WemoSensor(device);
            wemoSensor.on("BinaryState", function (value) {
                var oldValue = lastBinaryState[this.device.uuid];
                lastBinaryState[this.device.uuid] = value;
                if (!notify) return;
                if (oldValue != value || value == 1) {
                    clearTimeout(timeout);
                    timeout = setTimeout(restart, restartTime);
                    var url = urlBase + escape(this.device.deviceType + ":" + this.device.friendlyName + ":" + value);
                    //console.log(url);
                    console.log("{Motion" + (value == 1 ? " " : "Stopped ") + this.device.friendlyName + "}");
                    var req = http.get(url, function (res) { });
                    req.on("error", function (e) { console.log("http.get error " + e.message); });
                    req.setTimeout(500, function () { req.abort(); });
                }
            });
            devices.push(wemoSensor);
            break;
    }
};

http.createServer(onRequest).listen(60614);
function onRequest(req, res) {
    var reqContent = unescape(req.url).toLowerCase();
    var response = { request: reqContent, actions: [] };
    console.log("{" + reqContent + "}");
    if (reqContent == "/?restart" || reqContent == "/restart") {
        restart();
        response.actions.push("Restart Requested");
    }
    if (reqContent == "/?stop" || reqContent == "/stop") {
        notify = false;
        clearTimeout(timeout);
        clearTimeout(compoundTimeout);
        response.actions.push("Stopped Notifications");
    }
    if (reqContent == "/?start" || reqContent == "/start") {
        notify = true;
        clearTimeout(timeout);
        clearTimeout(compoundTimeout);
        timeout = setTimeout(restart, restartTime);
        compoundTimeout = setTimeout(restart, compoundTime);
        response.actions.push("Started Notifications");
    }
    if (reqContent == "/?status" || reqContent == "/status") {
        response.deviceCount = devices.length;
        response.switchCount = switches.length;
    }
    if (reqContent == "/devices" || reqContent == "/?devices") {
        response.deviceCount = devices.length;
        response.switchCount = switches.length;
        response.devices = [];
        var allDevicesLen = devices.length;
        for (var aDeviceIndex = 0; aDeviceIndex < allDevicesLen; aDeviceIndex++) {
            response.devices.push({
                Name: devices[aDeviceIndex].device.friendlyName,
                friendlyName: devices[aDeviceIndex].device.friendlyName,
                binaryState: lastBinaryState[devices[aDeviceIndex].device.uuid],
                IsOn: lastBinaryState[devices[aDeviceIndex].device.uuid] == 1,
                IsReachable: true,
                Brightnes: 255
            });
        }
    }
    if (reqContent == "/refresh" || reqContent.indexOf("/?refresh") == 0) {
        response.deviceCount = devices.length;
        response.switchCount = switches.length;
        var switchesLen = switches.length;
        for (var aSwitchindex = 0; aSwitchindex < switchesLen; aSwitchindex++) {
            response.actions.push("Refreshed " + switches[aSwitchindex].device.friendlyName);
            switches[aSwitchindex].getBinaryState();
        }
    }
    try {
        var devicesLen = switches.length;
        for (var deviceIndex = 0; deviceIndex < devicesLen; deviceIndex++) {
            var device = switches[deviceIndex];
            if (reqContent.indexOf(device.device.friendlyName.toLowerCase()) > -1) {
                response.device = device.device.friendlyName;
                if (reqContent.indexOf("turnon") > -1) {
                    response.actions.push("Turned On " + device.device.friendlyName);
                    device.setBinaryState(1);
                } else if (reqContent.indexOf("turnoff") > -1) {
                    response.actions.push("Turned Off " + device.device.friendlyName);
                    device.setBinaryState(0);
                } else if (reqContent.indexOf("binarystate") > -1) {
                    response.binaryState = lastBinaryState[device.device.uuid];
                    device.setBinaryState(0);
                } else if (reqContent.indexOf("info") > -1) {
                    response.actions.push("Retrieved Info " + device.device.friendlyName);
                    var deviceInfo = {
                        Name: devices[aDeviceIndex].device.friendlyName,
                        friendlyName: devices[aDeviceIndex].device.friendlyName,
                        binaryState: lastBinaryState[devices[aDeviceIndex].device.uuid],
                        IsOn: lastBinaryState[devices[aDeviceIndex].device.uuid] == 1,
                        IsReachable: true,
                        Brightnes: 255
                    }
                    response.devices.push(deviceInfo);
                }
                break;
            }
        }
    } catch (ex) {
        response.exception = ex;
    }
    response.restart = remoteRestart;
    response.notify = notify;
    res.write(JSON.stringify(response));
    res.end();
}

var remoteRestart = false;
var notify = true;
var restartTime = 15 * 60 * 1000;
var compoundTime = restartTime * 4.25;
var timeout = setTimeout(restart, restartTime);
var compoundTimeout = setTimeout(restart, compoundTime);

var cp = new UpnpControlPoint();
cp.on("device", handleDevice);
cp.search(null);

console.log(new Date() + " Running WemoService");

function restart() {
    remoteRestart = true;
    console.log(new Date() + " WemoService Restart Requested");
}
