import { useEffect, useState } from "react";
import moment from "moment";

export function TimeAgo(props) {
    const [content, setContent] = useState();

    useEffect(() => {
        let t = setInterval(() => {
            let ago = moment(props.from).fromNow();
            setContent(ago);
        }, 1000);
        return () => clearInterval(t);
    }, []);

    return (
        <small>{content}</small>
    );
}