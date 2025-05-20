class BlazorPanzoomInterop {

	constructor() {
	}

	fire(dispatcher, eventName, args) {
		const name = "On" + eventName + "Event"
		dispatcher.invokeMethodAsync(name, args)
	}

	createPanzoomForReference(element, options) {
		const panzoom = Panzoom(element, options)
		panzoom.boundElement = element
		return panzoom
	}

	createPanzoomForSelector(selector, options) {
		try {
			const elements = document.querySelectorAll(selector)
			const array = []
			for (let i = 0; i < elements.length; i++) {
				const element = elements[i]
				array.push(DotNet.createJSObjectReference(this.createPanzoomForReference(element, options)))
			}
			return array
		} catch {
			throw new Error(`Cannot create a Panzoom object from selectors!`);
		}
	}

	performForAllPanzoom(functionName, panzoomList, args) {
		if (!panzoomList) {
			return
		}

		for (let i = 0; i < panzoomList.length; i++) {
			const ref = panzoomList[i]
			ref[functionName](args)
		}
	}

	registerSetTransform(dotnetReference, panzoom) {
		if (!panzoom) {
			return
		}
		const opts = {
			setTransform: (elem, {x, y, scale, isSVG}) => {
				this.fire(dotnetReference, 'SetTransform', {x, y, scale, isSVG})
			}
		}
		panzoom.setOptions(opts)
	}

	registerZoomWithWheel(panzoom, element) {
		const targetElement = element || (panzoom ? panzoom.boundElement : null);
		if (!targetElement) {
			console.warn("registerZoomWithWheel: Target element not found.");
			return;
		}

		const parent = targetElement.parentElement;
		if (parent) {
			parent.addEventListener('wheel', panzoom.zoomWithWheel);
		} else {
			console.warn("registerZoomWithWheel: Target element has no parentElement.");
		}
	}

	registerWheelListener(dotnetReference, panzoom, element) {
		const targetElement = element || (panzoom ? panzoom.boundElement : null);
		if (!targetElement) {
			console.warn("registerWheelListener: Target element not found.");
			return;
		}

		const parent = targetElement.parentElement;
		if (parent) {
			if (panzoom.boundWheelListener) {
				parent.removeEventListener('wheel', panzoom.boundWheelListener);
			}
			panzoom.boundWheelListener = this.wheelHandler.bind(this, dotnetReference);
			parent.addEventListener('wheel', panzoom.boundWheelListener);
		} else {
			console.warn("registerWheelListener: Target element has no parentElement.");
		}
	}


	wheelHandler(dotnetReference, event) {
		event.preventDefault()
		this.fire(dotnetReference, 'CustomWheel', {
			deltaX: event.deltaX,
			deltaY: event.deltaY,
			clientX: event.clientX,
			clientY: event.clientY,
			shiftKey: event.shiftKey,
			altKey: event.altKey
		})
	}

	removeZoomWithWheel(panzoom, element) {
		const targetElement = element || (panzoom ? panzoom.boundElement : null);

		if (!targetElement || !panzoom) {
			return;
		}

		const parent = targetElement.parentElement;
		if (!parent) {
			return;
		}

		if (typeof panzoom.zoomWithWheel === 'function') {
			parent.removeEventListener('wheel', panzoom.zoomWithWheel);
		}
	}

	removeWheelListener(panzoom, element) {
		const targetElement = element || (panzoom ? panzoom.boundElement : null);

		if (!targetElement || !panzoom) {
			return;
		}

		const parent = targetElement.parentElement;
		if (!parent) {
			return;
		}

		if (panzoom && typeof panzoom.boundWheelListener === 'function') {
			parent.removeEventListener('wheel', panzoom.boundWheelListener);
			delete panzoom.boundWheelListener;
		}
	}

	destroyPanzoom(panzoom) {
		if (panzoom) {
			if (typeof panzoom.destroy === 'function') {
				panzoom.destroy();
			}

			delete panzoom.boundElement;
			delete panzoom.boundWheelListener;
		}
	}
}

window.blazorPanzoom = new BlazorPanzoomInterop()