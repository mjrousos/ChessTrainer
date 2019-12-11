import { MDCRipple, MDCRippleFoundation, util } from '@material/ripple';
import { MDCTextField } from '@material/textfield';
import { MDCTopAppBar } from '@material/top-app-bar';
import { MDCDrawer } from "@material/drawer";
import { MDCSnackbar } from '@material/snackbar';
import '@fortawesome/fontawesome-free/js/fontawesome';
import '@fortawesome/fontawesome-free/js/solid';

var primaryNotifier;
var copyNotifier;
var copyButton;

window.onload = function (e) {
    console.log('App loaded');
};

window.attachMDC = () => {
    const buttons = document.querySelectorAll('.mdc-button');
    for (const button of buttons) {
        MDCRipple.attachTo(button);
    }

    const textfields = document.querySelectorAll('.mdc-text-field');
    for (const textfield of textfields) {
        new MDCTextField(textfield);
    }

    const appBars = document.querySelectorAll('.mdc-top-app-bar');
    for (const appBar of appBars) {
        new MDCTopAppBar(appBar);
    }

    const drawers = document.querySelectorAll('.mdc-drawer');
    for (const drawer of drawers) {
        MDCDrawer.attachTo(drawer);
    }

    primaryNotifier = new MDCSnackbar(document.getElementById('PrimaryNotifier'));
    copyNotifier = new MDCSnackbar(document.getElementById('CopyNotifier'));
    copyNotifier.timeoutMs = 10000;
    copyButton = document.getElementById("CopyButton");
};

window.toggleDrawer = (drawerName) => {
    var drawer = MDCDrawer.attachTo(document.querySelector('#' + drawerName));
    drawer.open = !drawer.open;
};

window.getBoundingRectangle = (componentId) => {
    var element = document.getElementById(componentId);
    if (element) {
        console.log(element.getBoundingClientRect());
        var rect = element.getBoundingClientRect();
        return {
            // `| 0` truncates to int
            top: rect.top | 0,
            y: rect.top | 0,
            left: rect.left | 0,
            x: rect.left | 0,
            width: rect.width | 0,
            height: rect.height | 0,
            bottom: rect.bottom | 0,
            right: rect.right | 0
        };
    } else {
        return null;
    }
};

window.notifyWithCopy = (text, copyText) => {
    copyNotifier.labelText = text;
    copyButton.onclick = () => {
        window.copyToClipboard(text);
        window.notify(copyText);
    };
    copyNotifier.open();
};

window.notify = (text) => {
    primaryNotifier.labelText = text;
    primaryNotifier.open();
};

window.copyToClipboard = (text) => {
    // Note that this doesn't work with (pre-beta) Edge or Safari
    // If I want to support browsers other than Chrome, FireFox, and Edge Beta,
    // I'll need to replace this with a more general solution.
    // https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText
    return navigator.clipboard.writeText(text);
};
