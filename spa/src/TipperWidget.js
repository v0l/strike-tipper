import { createRef, useEffect, useState } from "react";
import { useSelector, useDispatch } from "react-redux";
import { setInvoice, addPaymentHistory } from "./WidgetState";
import { Events } from "./Events";

import QRCodeStyling from "qr-code-styling";

import "./TipperWidget.css";
import StrikeLogo from "./strike.svg";

export function TipperWidget(props) {
    const dispatch = useDispatch();

    let invoice = useSelector((state) => state.widget.invoice);
    let paymentHistory = useSelector((state) => state.widget.paymentHistory);

    let [loadingInvoice, setLoadingInvoice] = useState(false);
    let [ws, setWs] = useState();
    let qrRef = createRef();

    async function loadInvoice(id) {
        setLoadingInvoice(true);
        let req = await fetch(`/invoice/${id}`);
        if(req.ok) {
            let inv = await req.json();
            dispatch(setInvoice(inv));
        }
    }
    
    async function getNewInvoice(handle) {
        setLoadingInvoice(true);
        let body = {
            handle,
            description: "stream tip",
            amount: {
                currency: "USD",
                amount: 1.0
            }
        };
        
        let req = await fetch(`/invoice/new`, {
            method: "POST",
            body: JSON.stringify(body),
            headers: {
                "content-type": "application/json"
            }
        });
        if(req.ok) {
            let inv = await req.json();
            dispatch(setInvoice(inv));
        }
    }
    
    function handleWidgetEvent(ev) {
        if (ev.type === "InvoicePaid") {
            dispatch(addPaymentHistory(ev.data));
            getNewInvoice(props.username);
        }
    }
    
    function renderPayLog(l) {
        let pd = new Date(l.paid);
        return (
            <div className="tip-msg" key={l.paid}>
                <small>{pd.getHours()}:{pd.getMinutes().toFixed(0).padStart(2, '0')}</small>{l.from ?? "⚡"} tipped {l.currency} {l.amount.toLocaleString()}
            </div>
        )
    };

    useEffect(() => {
        if (invoice) {
            let expire = new Date(invoice.quote.expiration);
            let diff = expire.getTime() - new Date().getTime();
            let id = invoice.invoice.invoiceId;

            let t = setTimeout(() => {
                loadInvoice(id);
            }, diff);
            console.log(`Set timout for: ${diff}ms`);

            var qr = new QRCodeStyling({
                width: 600,
                height: 600,
                data: `lightning:${invoice.quote.lnInvoice}`,
                image: StrikeLogo,
                margin: 0,
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

            setLoadingInvoice(false);
            Events.AddListenId(id)
            return () => clearInterval(t);
        }
    }, [invoice]);
        
    useEffect(() => {
        getNewInvoice(props.username);
        Events.HookEvent("WidgetEvent", (d) => handleWidgetEvent(d.data));
    }, []);
    
    return (
        <div className="widget">
            {invoice ? <div ref={qrRef} className={loadingInvoice ? "qr-loading" : ""}></div> : <b>Loading...</b>}
            <div className="tip-history">
                {[...(paymentHistory || [])].sort((a, b) => {
                    let ad = new Date(a.paid).getTime();
                    var bd = new Date(b.paid).getTime();
                    return bd - ad;
                }).map(a => renderPayLog(a))}
            </div>
        </div>
    );
}