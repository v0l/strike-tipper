import { createSlice } from '@reduxjs/toolkit'

export const widgetSlice = createSlice({
    name: 'widget',
    initialState: {
        paymentHistory: [],
        invoice: null
    },
    reducers: {
        setInvoice: (state, action) => {
            state.invoice = action.payload;
        },
        addPaymentHistory: (state, action) => {
            state.paymentHistory = [
                ...state.paymentHistory,
                action.payload
            ].slice(-5);
        }
    },
})

// Action creators are generated for each case reducer function
export const { setInvoice, addPaymentHistory } = widgetSlice.actions

export default widgetSlice.reducer