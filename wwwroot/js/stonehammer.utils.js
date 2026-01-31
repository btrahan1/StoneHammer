/**
 * StoneHammer v10.0: Utils Module
 * Utility functions for property access, logging, and vector parsing.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.getProp = function (obj, key) {
        if (!obj) return null;
        return obj[key] ?? obj[key.charAt(0).toLowerCase() + key.slice(1)] ?? obj[key.toUpperCase()] ?? null;
    };

    sh.log = function (msg, color = "white") {
        const el = document.getElementById("debug-log");
        if (el) {
            const div = document.createElement("div");
            div.style.color = color;
            div.innerText = "[" + new Date().toLocaleTimeString() + "] " + msg;
            el.prepend(div);
        }
        console.log("StoneHammer: " + msg);
    };

    sh.parseVec3 = function (data, defaultVal = { x: 0, y: 0, z: 0 }) {
        if (!data) return new BABYLON.Vector3(defaultVal.x, defaultVal.y, defaultVal.z);
        if (Array.isArray(data)) return new BABYLON.Vector3(data[0] ?? defaultVal.x, data[1] ?? defaultVal.y, data[2] ?? defaultVal.z);
        if (typeof data === 'object') return new BABYLON.Vector3(data.x ?? data.X ?? defaultVal.x, data.y ?? data.Y ?? defaultVal.y, data.z ?? data.Z ?? defaultVal.z);
        return new BABYLON.Vector3(defaultVal.x, defaultVal.y, defaultVal.z);
    };
})();
