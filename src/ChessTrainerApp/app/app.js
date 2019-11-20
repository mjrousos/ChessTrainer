import { MDCRipple, MDCRippleFoundation, util } from '@material/ripple';
import { MDCTextField } from '@material/textfield';
import { MDCTopAppBar } from '@material/top-app-bar';
import { MDCDrawer } from "@material/drawer";
import '@fortawesome/fontawesome-free/js/fontawesome';
import '@fortawesome/fontawesome-free/js/solid';

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
};

window.toggleDrawer = (drawerName) => {
    var drawer = MDCDrawer.attachTo(document.querySelector('#' + drawerName));
    drawer.open = !drawer.open;
}