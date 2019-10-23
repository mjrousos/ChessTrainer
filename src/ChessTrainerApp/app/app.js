import './favicon.ico';
import { MDCRipple, MDCRippleFoundation, util } from '@material/ripple';
import { MDCTextField } from '@material/textfield';

window.onload = function (e) {
    console.log('App loaded');
};

window.AttachMDC = () => {
    const buttons = document.querySelectorAll('.mdc-button');
    for (const button of buttons) {
        MDCRipple.attachTo(button);
    }

    const textfields = document.querySelectorAll('.mdc-text-field');
    for (const textfield of textfields) {
        new MDCTextField(textfield);
    }
};