import {createRef, useEffect, useState} from "react";
import QRCodeStyling from "qr-code-styling";

import "./TipperWidget.css";
import StrikeLogo from "./strike.svg";

export function TipperWidget(props) {
    let [invoice, setInvoice] = useState();
    let [payHistory, setPayHistory] = useState([]);
    let qrRef = createRef();
    
    async function loadInvoice(id) {
        let req = await fetch(`/invoice/${id}`);
        if(req.ok) {
            let inv = await req.json();
            setInvoice(inv);
        }
    }
    
    async function getNewInvoice(handle) {
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
            setInvoice(inv);
        }
    }
    
    function handleWidgetEvent(ev) {
        if(ev.type === "InvoicePaid") {
            setPayHistory([
                ...payHistory,
                ev.data
            ].slice(-5));
        }
    }
    
    function renderPayLog(l) {
        return (
            <div className="tip-msg">
                {l.from ?? "⚡"} tipped {l.currency} {l.amount.toLocaleString()}
            </div>
        )
    };
    
    useEffect(() => {
        if(invoice) {
            let expire = new Date(invoice.quote.expiration);
            let diff = expire.getTime() - new Date().getTime();
            
            let t = setTimeout(() => {
                loadInvoice(invoice.invoice.invoiceId);
            }, diff);
            console.log(`Set timout for: ${diff}ms`);

            var qr = new QRCodeStyling({
                width: 300,
                height: 300,
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

            return () => clearInterval(t);
        }
    }, [invoice]);
    
    useEffect(() => {
        if(invoice) {
            let proto = window.location.protocol === "https:" ? "wss:" : "ws:";
            let ws = new WebSocket(`${proto}//${window.location.host}/events`);
            ws.onopen = function (ev) {
                console.log("Events websocket open!");
                ws.send(JSON.stringify({
                    cmd: "listen",
                    args: [invoice.invoice.invoiceId]
                }));
            }
            ws.onclose = function (ev) {
                console.log(ev);
            }
            ws.onerror = function (ev) {
                console.log(ev);
            }
            ws.onmessage = function (ev) {
                let msg = JSON.parse(ev.data);
                console.log(msg);
                if(msg.type === "WidgetEvent"){
                    handleWidgetEvent(msg.data);
                }
            }
            return () => ws.close();
        }
    }, [invoice]);
    
    useEffect(() => {
        getNewInvoice(props.username)
    }, []);
    
    return (
        <div className="widget">
            {invoice ? <div ref={qrRef}></div> : <b>Loading...</b>}
            <div className="tip-history">
                {payHistory.map(a => renderPayLog(a))}
            </div>
        </div>
    );
}