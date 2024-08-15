window.printJS = function (base64pdf) {
    try {
        var blob = base64toBlob(base64pdf, 'application/pdf');
        var url = URL.createObjectURL(blob);

        // Open PDF in a new tab/window
        var newTab = window.open(url, '_blank');
        if (newTab) {
            newTab.focus();
        } else {
            alert('Your browser blocked opening a new tab. Please check your browser settings.');
        }
    } catch (error) {
        console.error('Error in printJS:', error);
    }
}

window.saveAsFile = function (fileName, byteBase64) {
    var link = document.createElement('a');
    link.href = "data:application/octet-stream;base64," + byteBase64;
    link.download = fileName;
    link.click();
};

function base64toBlob(base64, type) {
    var byteString = atob(base64);
    var ab = new ArrayBuffer(byteString.length);
    var ia = new Uint8Array(ab);
    for (var i = 0; i < byteString.length; i++) {
        ia[i] = byteString.charCodeAt(i);
    }
    return new Blob([ab], { type: type });
}
