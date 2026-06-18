mergeInto(LibraryManager.library, {
    TriggerVR: function () {
        var button = document.getElementById('entervr');
        if (button) {
            button.click();
        }
    }
});