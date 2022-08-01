import QRCodeStyling from "qr-code-styling";
import StrikeLogo from "./strike.svg";
import {useEffect, useRef} from "react";

export default function TipperQR(props) {
    const qrRef = useRef();

    useEffect(() => {
        if (props.link) {
            let qr = new QRCodeStyling({
                width: 600,
                height: 600,
                data: props.link,
                image: StrikeLogo,
                margin: 5,
                type: 'canvas',
                imageOptions: {
                    hideBackgroundDots: false
                },
                dotsOptions: {
                    type: 'rounded'
                },
                cornersSquareOptions: {
                    type: 'extra-rounded'
                }
            });
            qrRef.current.innerHTML = "";
            qr.append(qrRef.current);
            qrRef.current.onclick = function (e) {
                let elm = document.createElement("a");
                elm.href = props.link;
                elm.click();
            }
        }
    }, [props.link]);

    return (
        <div ref={qrRef} className={props.blur ? "qr-loading" : ""}></div>
    );
}