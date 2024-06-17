function setClickOnElement(element) {
    element.value = null;
    element.click();
}

function getElementFileInfo(element) {
    return {
        lastModified: element.files[0].lastModified,
        lastModifiedDate: element.files[0].lastModifiedDate,
        name: element.files[0].name,
        size: element.files[0].size,
        type: element.files[0].type
    };

}

function openBase64InNewTab(data, dataType) {
    var byteCharacters = atob(data);
    var byteNumbers = new Array(byteCharacters.length);
    for (var i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    var byteArray = new Uint8Array(byteNumbers);
    var file = new Blob([byteArray], { type: dataType });
    var fileURL = URL.createObjectURL(file);
    window.open(fileURL);
}

function BlazorFocusElement(element) {
    if (element instanceof HTMLElement) {
        element.focus();
    }
}

function BlazoredTypeaheadFocus() {
    setTimeout(function () {
        document.getElementsByClassName("blazored-typeahead__input")[0].focus()
    }, 100)
}

function getWindowsLocationOrigin() {
    return window.location.origin
}

function addDocumentListener(dotnetHelper, eventListener, methodName) {
    document.addEventListener(eventListener, function (e) {
        //console.log("hey you document", e)
        dotnetHelper.invokeMethodAsync(methodName)
    })
}

function printJsBase64Pdf(base64) {
    printJS({ printable: base64, type: 'pdf', base64: true })
}



//function openLink(base64) {
//    window.open()
//}